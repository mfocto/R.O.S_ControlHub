using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using ROS_ControlHub.Adapters.Abstractions;
using Microsoft.Extensions.Configuration;

namespace ROS_ControlHub.Adapters.Real;

/// <summary>
/// Kepware OPC UA 서버에 연결하는 클라이언트 어댑터
/// 
/// [작성 이유]
/// - Kepware는 산업용 OPC 서버로, 다양한 PLC/설비와 연결하여 OPC UA 서버로 제공합니다.
/// - 이 어댑터는 Kepware OPC UA 서버에 연결하여 디바이스 상태를 읽고 제어 명령을 전송합니다.
/// - 자동 재연결, Thread-safe 연결 관리, 에러 처리 등의 안정적인 통신을 보장합니다.
/// 
/// [설정 파일 참조]
/// appsettings.json의 "OpcUa" 섹션에서 다음 설정을 읽습니다:
/// - EndpointUrl: Kepware OPC UA 서버 주소 (예: "opc.tcp://127.0.0.1:49320")
/// - SessionName: OPC UA 세션 이름
/// - SecurityMode: 보안 모드 ("None" 또는 "SignAndEncrypt")
/// - SecurityPolicy: 보안 정책 ("None" 또는 "Basic256Sha256")
/// - ReconnectIntervalMs: 재연결 시도 간격 (밀리초)
/// - ConnectionTimeoutMs: 연결 타임아웃 (밀리초)
/// - SessionTimeoutMs: 세션 타임아웃 (밀리초)
/// - AutoAcceptUntrustedCertificates: 신뢰되지 않은 인증서 자동 수락 여부
/// - Kepware.Channel: Kepware 채널 이름 (예: "Channel1")
/// - Kepware.Device: Kepware 디바이스 이름 (예: "Device1")
/// - Kepware.Tags: 읽기/쓰기할 태그 이름들
/// </summary>
public class OpcUaClientAdapter : IOpcUaAdapter, IDisposable
{
    // 로깅을 위한 로거 - Kepware 연결 상태, 에러 등을 기록
    private readonly ILogger<OpcUaClientAdapter> _logger;
    
    // 설정 파일에서 Kepware 연결 정보를 읽기 위한 Configuration 객체
    private readonly IConfiguration _configuration;
    
    // Kepware OPC UA 서버의 엔드포인트 URL (예: "opc.tcp://127.0.0.1:49320")
    // appsettings.json의 "OpcUa:EndpointUrl"에서 읽음
    private readonly string _endpointUrl;
    
    // OPC UA 세션 이름 - Kepware 서버에서 이 클라이언트를 식별하는 이름
    // appsettings.json의 "OpcUa:SessionName"에서 읽음
    private readonly string _sessionName;
    
    // Kepware 채널 이름 - Kepware에서 여러 PLC를 그룹화하는 단위
    // appsettings.json의 "OpcUa:Kepware:Channel"에서 읽음 (예: "Channel1")
    private readonly string _channel;
    
    // Kepware 디바이스 이름 - 특정 PLC/설비를 식별하는 이름
    // appsettings.json의 "OpcUa:Kepware:Device"에서 읽음 (예: "Device1")
    private readonly string _device;
    
    // 재연결 시도 간격 (밀리초) - 연결이 끊어졌을 때 재연결을 시도하는 주기
    // appsettings.json의 "OpcUa:ReconnectIntervalMs"에서 읽음 (기본값: 5000ms)
    private readonly int _reconnectIntervalMs;
    
    // 연결 타임아웃 (밀리초) - Kepware 서버에 연결을 시도할 때 최대 대기 시간
    // appsettings.json의 "OpcUa:ConnectionTimeoutMs"에서 읽음 (기본값: 15000ms)
    private readonly int _connectionTimeoutMs;
    
    // OPC UA 세션 객체 - Kepware 서버와의 실제 통신 세션
    // null일 수 있으므로 nullable로 선언
    private Session? _session;
    
    // OPC UA 클라이언트 애플리케이션 설정 - 인증서, 보안 정책 등 포함
    private readonly ApplicationConfiguration _config;
    
    // 연결 시도 시 동시성 제어를 위한 세마포어
    // 여러 스레드에서 동시에 연결을 시도하는 것을 방지하여 Thread-safe 보장
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    
    // 재연결 작업을 취소하기 위한 CancellationTokenSource
    // 연결이 성공하면 재연결 작업을 취소하기 위해 사용
    private CancellationTokenSource? _reconnectCts;

    /// <summary>
    /// 생성자 - 설정 파일에서 Kepware 연결 정보를 읽어 초기화
    /// 
    /// [작성 이유]
    /// - DI(Dependency Injection)를 통해 로거와 설정을 주입받습니다.
    /// - appsettings.json에서 Kepware 연결에 필요한 모든 설정을 읽어 필드에 저장합니다.
    /// - 기본값을 제공하여 설정이 없어도 동작할 수 있도록 합니다.
    /// </summary>
    public OpcUaClientAdapter(ILogger<OpcUaClientAdapter> logger, IConfiguration config)
    {
        _logger = logger;
        _configuration = config;
        
        // Kepware OPC UA 서버 주소 - 기본값은 로컬호스트의 Kepware 기본 포트
        _endpointUrl = config.GetValue("OpcUa:EndpointUrl", "opc.tcp://127.0.0.1:49320")!;
        
        // OPC UA 세션 이름 - Kepware 서버에서 이 클라이언트를 식별
        _sessionName = config.GetValue("OpcUa:SessionName", "ROS_ControlHub_Client")!;
        
        // Kepware 채널 이름 - Kepware 프로젝트에서 정의한 채널 이름
        // Node ID 형식: ns=2;s=Channel1.Device1.TagName
        _channel = config.GetValue("OpcUa:Kepware:Channel", "Channel1")!;
        
        // Kepware 디바이스 이름 - 특정 PLC/설비를 식별
        _device = config.GetValue("OpcUa:Kepware:Device", "Device1")!;
        
        // 재연결 간격 - 연결 실패 시 5초마다 재시도 (기본값)
        _reconnectIntervalMs = config.GetValue("OpcUa:ReconnectIntervalMs", 5000);
        
        // 연결 타임아웃 - 15초 내에 연결되지 않으면 실패로 간주 (기본값)
        _connectionTimeoutMs = config.GetValue("OpcUa:ConnectionTimeoutMs", 15000);

        // OPC UA 클라이언트 설정 생성 (인증서, 보안 정책 등)
        _config = CreateOpcUaConfiguration();
    }

    /// <summary>
    /// OPC UA 클라이언트 애플리케이션 설정 생성
    /// 
    /// [작성 이유]
    /// - OPC UA 클라이언트가 Kepware 서버에 연결하기 위해 필요한 모든 설정을 구성합니다.
    /// - 인증서 관리, 보안 정책, 타임아웃 등을 설정합니다.
    /// - 개발 환경에서는 신뢰되지 않은 인증서를 자동으로 수락하도록 설정할 수 있습니다.
    /// </summary>
    private ApplicationConfiguration CreateOpcUaConfiguration()
    {
        // 신뢰되지 않은 인증서 자동 수락 여부 - 개발 환경에서는 true로 설정
        // 프로덕션 환경에서는 false로 설정하고 인증서를 신뢰 목록에 추가해야 함
        var autoAcceptCerts = _configuration.GetValue("OpcUa:AutoAcceptUntrustedCertificates", true);
        
        // 세션 타임아웃 - 60초 동안 통신이 없으면 세션이 만료됨 (기본값)
        var sessionTimeout = _configuration.GetValue("OpcUa:SessionTimeoutMs", 60000);

        var config = new ApplicationConfiguration
        {
            ApplicationName = "ROS_ControlHub",
            ApplicationType = ApplicationType.Client, // OPC UA 클라이언트로 동작
            
            // 보안 설정 - 인증서 관리 및 보안 정책
            SecurityConfiguration = new SecurityConfiguration
            {
                // 클라이언트 애플리케이션 인증서 저장 위치
                // 이 인증서는 Kepware 서버에 자신을 인증하는 데 사용됨
                ApplicationCertificate = new CertificateIdentifier 
                { 
                    StoreType = "Directory", 
                    StorePath = @"%CommonApplicationData%\ROS_ControlHub\pki\own", 
                    SubjectName = "ROS_ControlHub" 
                },
                
                // 신뢰할 수 있는 서버 인증서 저장 위치
                // Kepware 서버의 인증서가 여기에 저장되면 자동으로 신뢰됨
                TrustedPeerCertificates = new CertificateTrustList 
                { 
                    StoreType = "Directory", 
                    StorePath = @"%CommonApplicationData%\ROS_ControlHub\pki\trusted" 
                },
                
                // 거부된 인증서 저장 위치
                RejectedCertificateStore = new CertificateTrustList 
                { 
                    StoreType = "Directory", 
                    StorePath = @"%CommonApplicationData%\ROS_ControlHub\pki\rejected" 
                },
                
                // 신뢰되지 않은 인증서 자동 수락 - 개발 환경 편의를 위해
                AutoAcceptUntrustedCertificates = autoAcceptCerts,
                
                // 애플리케이션 인증서를 신뢰 목록에 자동 추가
                AddAppCertToTrustedStore = true
            },
            
            TransportConfigurations = new TransportConfigurationCollection(),
            
            // 전송 할당량 설정 - 연결 타임아웃 등
            TransportQuotas = new TransportQuotas 
            { 
                // OPC UA 작업(읽기/쓰기)의 최대 대기 시간
                OperationTimeout = _connectionTimeoutMs 
            },
            
            // 클라이언트 설정 - 세션 타임아웃 등
            ClientConfiguration = new ClientConfiguration 
            { 
                // 세션이 유지되는 최대 시간 (밀리초)
                // 이 시간 동안 통신이 없으면 세션이 자동으로 종료됨
                DefaultSessionTimeout = sessionTimeout 
            }
        };

        // 설정 검증 - 필수 설정이 올바르게 구성되었는지 확인
        // ValidateAsync를 사용하여 비동기로 검증 (권장 방식)
        config.ValidateAsync(ApplicationType.Client).GetAwaiter().GetResult();

        // 인증서 검증 이벤트 핸들러
        // Kepware 서버의 인증서가 신뢰되지 않을 때 자동으로 수락하도록 설정
        config.CertificateValidator.CertificateValidation += (s, e) =>
        {
            if (e.Error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted && autoAcceptCerts)
            {
                _logger.LogWarning("Accepting untrusted Kepware server certificate: {Subject}", e.Certificate.Subject);
                e.Accept = true; // 인증서를 수락하여 연결 계속 진행
            }
        };

        return config;
    }

    /// <summary>
    /// Kepware OPC UA 서버에 연결 (재연결 로직 포함)
    /// 
    /// [작성 이유]
    /// - 연결이 끊어졌을 때 자동으로 재연결을 시도합니다.
    /// - SemaphoreSlim을 사용하여 여러 스레드에서 동시에 연결을 시도하는 것을 방지합니다.
    /// - 이중 체크 패턴을 사용하여 불필요한 연결 시도를 방지합니다.
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        // 이미 연결되어 있으면 바로 반환 (성능 최적화)
        if (_session != null && _session.Connected) return;

        // 동시성 제어 - 여러 스레드에서 동시에 연결을 시도하는 것을 방지
        // 이렇게 하지 않으면 여러 세션이 생성되어 리소스 낭비 및 오류 발생 가능
        await _connectionLock.WaitAsync(ct);
        try
        {
            // 이중 체크 (Double-Check Locking 패턴)
            // 락을 획득한 후 다시 확인하여 다른 스레드가 이미 연결했는지 확인
            if (_session != null && _session.Connected) return;

            // 기존 세션이 있으면 정리 - 메모리 누수 방지
            if (_session != null)
            {
                try
                {
                    // 연결된 세션은 먼저 닫아야 함
                    if (_session.Connected)
                    {
                        _session.CloseAsync().GetAwaiter().GetResult();
                    }
                    _session.Dispose();
                }
                catch { } // 정리 중 에러는 무시 (이미 끊어진 세션일 수 있음)
                _session = null;
            }

            _logger.LogInformation("Connecting to Kepware OPC UA Server at {Url}...", _endpointUrl);
            
            // 보안 모드 설정 - "None"은 암호화 없이 통신 (개발 환경용)
            // 프로덕션에서는 "SignAndEncrypt" 사용 권장
            var securityMode = _configuration.GetValue("OpcUa:SecurityMode", "None");
            
            // 보안 정책 설정 - "None"은 보안 정책 없음 (개발 환경용)
            // 프로덕션에서는 "Basic256Sha256" 등 사용 권장
            var securityPolicy = _configuration.GetValue("OpcUa:SecurityPolicy", "None");
            
            // Kepware 엔드포인트 구성 - 서버의 연결 정보 설정
            var endpoint = new EndpointDescription
            {
                EndpointUrl = _endpointUrl, // Kepware 서버 주소
                
                // 보안 모드 설정 - None 또는 SignAndEncrypt
                SecurityMode = securityMode == "None" ? MessageSecurityMode.None : MessageSecurityMode.SignAndEncrypt,
                
                // 보안 정책 URI - None 또는 Basic256Sha256
                SecurityPolicyUri = securityPolicy == "None" ? SecurityPolicies.None : SecurityPolicies.Basic256Sha256,
                
                // 사용자 인증 토큰 정책 - Anonymous는 인증 없이 연결
                // 프로덕션에서는 Username/Password 또는 인증서 기반 인증 사용 권장
                UserIdentityTokens = new UserTokenPolicyCollection
                {
                    new UserTokenPolicy { TokenType = UserTokenType.Anonymous }
                },
                
                // 전송 프로토콜 - OPC UA TCP 전송 사용
                TransportProfileUri = Profiles.UaTcpTransport
            };
            
            // 엔드포인트 설정 생성
            var endpointConfiguration = EndpointConfiguration.Create(_config);
            var selectedEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);

            // 세션 타임아웃 설정
            var sessionTimeout = _configuration.GetValue("OpcUa:SessionTimeoutMs", 60000);
            
            // OPC UA 세션 생성 - Kepware 서버에 실제로 연결
            // Session.Create는 비동기 메서드이지만 동기적으로 호출 가능한 오버로드 사용
            _session = await Session.Create(
                _config,              // 애플리케이션 설정
                selectedEndpoint,     // Kepware 엔드포인트
                false,                // 클라이언트 설명 표시 여부
                _sessionName,         // 세션 이름
                (uint)sessionTimeout, // 세션 타임아웃 (밀리초)
                new UserIdentity(new AnonymousIdentityToken()), // 익명 인증
                null);                // 선호 로케일 (null = 기본값)
            
            _logger.LogInformation("Successfully connected to Kepware OPC UA Server at {Url}", _endpointUrl);
            
            // 재연결 취소 토큰 초기화 - 연결 성공 시 재연결 작업 중지
            _reconnectCts?.Cancel();
            _reconnectCts = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Kepware OPC UA Server at {Url}", _endpointUrl);
            
            // 재연결 시도 시작 - 연결 실패 시 자동으로 재연결 시도
            StartReconnectTask(ct);
            throw; // 예외를 다시 던져서 호출자에게 알림
        }
        finally
        {
            // 락 해제 - 예외가 발생해도 반드시 해제해야 함
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 연결 끊김 시 자동 재연결 시도
    /// 
    /// [작성 이유]
    /// - 네트워크 장애나 Kepware 서버 재시작 등으로 연결이 끊어졌을 때 자동으로 재연결을 시도합니다.
    /// - 백그라운드 태스크로 실행되어 메인 스레드를 블로킹하지 않습니다.
    /// - 설정된 간격(ReconnectIntervalMs)마다 재연결을 시도하여 안정적인 통신을 보장합니다.
    /// </summary>
    private void StartReconnectTask(CancellationToken ct)
    {
        // 이미 재연결 작업이 실행 중이면 중복 실행 방지
        if (_reconnectCts != null && !_reconnectCts.Token.IsCancellationRequested) return;

        // 재연결 작업을 취소할 수 있는 CancellationTokenSource 생성
        _reconnectCts = new CancellationTokenSource();
        
        // 외부 CancellationToken과 재연결 취소 토큰을 연결
        // 둘 중 하나라도 취소되면 재연결 작업 중지
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _reconnectCts.Token);

        // 백그라운드에서 재연결 시도 (fire-and-forget)
        // await하지 않으므로 메인 스레드를 블로킹하지 않음
        _ = Task.Run(async () =>
        {
            // 취소될 때까지 반복 시도
            while (!linkedCts.Token.IsCancellationRequested)
            {
                try
                {
                    // 설정된 간격만큼 대기 (기본값: 5초)
                    await Task.Delay(_reconnectIntervalMs, linkedCts.Token);
                    
                    _logger.LogInformation("Attempting to reconnect to Kepware OPC UA Server...");
                    
                    // 재연결 시도
                    await EnsureConnectedAsync(linkedCts.Token);
                    
                    // 연결 성공 시 루프 종료
                    break;
                }
                catch (OperationCanceledException)
                {
                    // 취소 요청이 있으면 루프 종료
                    break;
                }
                catch (Exception ex)
                {
                    // 재연결 실패 시 로그 기록 후 다음 시도까지 대기
                    _logger.LogWarning(ex, "Reconnection attempt failed. Retrying in {Interval}ms...", _reconnectIntervalMs);
                    // 루프가 계속되어 다음 시도까지 대기 후 다시 시도
                }
            }
        }, linkedCts.Token);
    }

    /// <summary>
    /// Kepware 태그의 Node ID 생성 (ns=2;s=Channel.Device.Tag 형식)
    /// 
    /// [작성 이유]
    /// - Kepware는 특정 Node ID 형식을 사용합니다: ns=2;s=Channel.Device.Tag
    /// - 이 메서드는 채널, 디바이스, 태그 이름을 조합하여 올바른 Node ID를 생성합니다.
    /// - 설정 파일에서 읽은 채널/디바이스 이름을 사용하여 유연성을 제공합니다.
    /// 
    /// [예시]
    /// - Channel: "Channel1", Device: "Device1", Tag: "Connected"
    /// - 결과: "ns=2;s=Channel1.Device1.Connected"
    /// </summary>
    private NodeId BuildKepwareNodeId(string tagName)
    {
        // Kepware의 일반적인 Node ID 형식: ns=2;s=Channel1.Device1.TagName
        // ns=2: 네임스페이스 인덱스 2 (Kepware의 기본 네임스페이스)
        // s=: 문자열 형식의 Node ID
        var nodeIdString = $"ns=2;s={_channel}.{_device}.{tagName}";
        return new NodeId(nodeIdString);
    }

    /// <summary>
    /// Kepware에서 디바이스 상태 읽기
    /// 
    /// [작성 이유]
    /// - StatePollingWorker에서 주기적으로 호출되어 Kepware의 디바이스 상태를 읽습니다.
    /// - 설정 파일에서 읽을 태그 이름을 가져와 유연성을 제공합니다.
    /// - 연결이 끊어진 경우 자동으로 재연결을 시도합니다.
    /// 
    /// [반환 데이터]
    /// - opc.connected: 디바이스 연결 상태 (bool)
    /// - opc.conveyor.running: 컨베이어 실행 상태 (bool)
    /// - opc.conveyor.speed: 컨베이어 속도 (double)
    /// - deviceName: 디바이스 이름 (예: "Kepware_Channel1_Device1")
    /// - deviceStatus: 디바이스 상태 ("Online" 또는 "Offline")
    /// </summary>
    public async Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct)
    {
        // 연결 확인 및 필요시 연결 시도
        await EnsureConnectedAsync(ct);

        // Kepware 태그 설정에서 읽을 태그 목록 가져오기
        // 설정 파일에 없으면 기본값 사용 (예: "Connected", "Running", "Speed")
        var connectedTag = _configuration.GetValue("OpcUa:Kepware:Tags:Connected", "Connected")!;
        var runningTag = _configuration.GetValue("OpcUa:Kepware:Tags:Running", "Running")!;
        var speedTag = _configuration.GetValue("OpcUa:Kepware:Tags:Speed", "Speed")!;

        // 읽을 Node ID 목록 생성
        // Kepware의 Node ID 형식으로 변환하여 사용
        var nodesToRead = new ReadValueIdCollection
        {
            new ReadValueId { NodeId = BuildKepwareNodeId(connectedTag), AttributeId = Attributes.Value },
            new ReadValueId { NodeId = BuildKepwareNodeId(runningTag), AttributeId = Attributes.Value },
            new ReadValueId { NodeId = BuildKepwareNodeId(speedTag), AttributeId = Attributes.Value }
        };

        try
        {
            // Kepware 서버에서 태그 값 읽기
            // null: 요청 헤더 (기본값 사용)
            // 0: 최대 연령 (0 = 캐시 사용 안 함)
            // TimestampsToReturn.Both: 소스 타임스탬프와 서버 타임스탬프 모두 반환
            var readResponse = await _session!.ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
                nodesToRead,
                ct);

            var results = readResponse.Results;
            var result = new Dictionary<string, object>();

            // Kepware 태그 값 읽기 및 결과 파싱
            // StatusCode.IsGood로 읽기 성공 여부 확인
            // 값이 null이면 기본값 사용 (false 또는 0.0)
            result["opc.connected"] = results != null && results.Count > 0 && StatusCode.IsGood(results[0].StatusCode) 
                ? results[0].Value ?? false 
                : false;
            result["opc.conveyor.running"] = results != null && results.Count > 1 && StatusCode.IsGood(results[1].StatusCode) 
                ? results[1].Value ?? false 
                : false;
            result["opc.conveyor.speed"] = results != null && results.Count > 2 && StatusCode.IsGood(results[2].StatusCode) 
                ? results[2].Value ?? 0.0 
                : 0.0;

            // 디바이스 이름 및 상태 설정
            result["deviceName"] = $"Kepware_{_channel}_{_device}";
            result["deviceStatus"] = (bool)result["opc.connected"] ? "Online" : "Offline";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read state from Kepware. NodeId pattern: {Channel}.{Device}", _channel, _device);
            
            // 연결이 끊어진 경우 재연결 시도
            // 다음 읽기 시도 시 자동으로 재연결됨
            if (_session == null || !_session.Connected)
            {
                _session = null;
                StartReconnectTask(ct);
            }
            
            throw; // 예외를 다시 던져서 호출자에게 알림
        }
    }

    /// <summary>
    /// Kepware에 제어 명령 쓰기
    /// 
    /// [작성 이유]
    /// - GrpcControlService나 WebRtcHub에서 호출되어 Kepware에 제어 명령을 전송합니다.
    /// - stateJson은 JSON 형식의 제어 명령입니다 (예: {"status": "start", "payload": {...}}).
    /// - 연결이 끊어진 경우 자동으로 재연결을 시도합니다.
    /// 
    /// [사용 예시]
    /// - deviceId: "robot-1"
    /// - stateJson: "{\"status\": \"start\", \"payload\": {}}"
    /// - 결과: Kepware의 ControlCommand 태그에 JSON 문자열이 기록됨
    /// </summary>
    public async Task WriteStateAsync(string deviceId, string stateJson)
    {
        // 연결 확인 및 필요시 연결 시도
        await EnsureConnectedAsync(CancellationToken.None);

        // 제어 명령을 쓸 태그 이름 가져오기 (기본값: "ControlCommand")
        var controlCommandTag = _configuration.GetValue("OpcUa:Kepware:Tags:ControlCommand", "ControlCommand")!;
        
        _logger.LogInformation("[Kepware OPC-UA Write] DeviceId: {DeviceId}, Channel: {Channel}, Device: {Device}, Tag: {Tag}, State: {State}", 
            deviceId, _channel, _device, controlCommandTag, stateJson);
        
        try
        {
            // Kepware Node ID 생성 (예: "ns=2;s=Channel1.Device1.ControlCommand")
            var nodeId = BuildKepwareNodeId(controlCommandTag);
            
            // 쓰기 값 생성
            var writeValue = new WriteValue
            {
                NodeId = nodeId,                    // 쓸 태그의 Node ID
                AttributeId = Attributes.Value,     // Value 속성에 쓰기
                Value = new DataValue(new Variant(stateJson)) // JSON 문자열을 Variant로 변환
            };

            var writeValues = new WriteValueCollection { writeValue };
            
            // Kepware 서버에 쓰기 요청
            // null: 요청 헤더 (기본값 사용)
            var writeResponse = await _session!.WriteAsync(null, writeValues, CancellationToken.None);
            var res = writeResponse.Results;

            // 쓰기 결과 확인
            if (res != null && res.Count > 0)
            {
                if (StatusCode.IsBad(res[0]))
                {
                    // 쓰기 실패 - StatusCode가 Bad이면 에러
                    _logger.LogWarning("Failed to write to Kepware OPC UA Node {NodeId}: {Status}", nodeId, res[0]);
                    throw new InvalidOperationException($"Write failed: {res[0]}");
                }
                else
                {
                    // 쓰기 성공
                    _logger.LogDebug("Successfully wrote to Kepware OPC UA Node {NodeId}", nodeId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write state to Kepware. DeviceId: {DeviceId}, NodeId pattern: {Channel}.{Device}.{Tag}", 
                deviceId, _channel, _device, controlCommandTag);
            
            // 연결이 끊어진 경우 재연결 시도
            // 다음 쓰기 시도 시 자동으로 재연결됨
            if (_session == null || !_session.Connected)
            {
                _session = null;
                StartReconnectTask(CancellationToken.None);
            }
            
            throw; // 예외를 다시 던져서 호출자에게 알림
        }
    }

    /// <summary>
    /// 리소스 정리 - Kepware 연결 종료 및 리소스 해제
    /// 
    /// [작성 이유]
    /// - IDisposable 인터페이스 구현 - 애플리케이션 종료 시 리소스 정리
    /// - 재연결 작업 취소, 세션 종료, 락 해제 등을 수행하여 메모리 누수 방지
    /// - 정상 종료 시 Kepware 서버에 연결 종료를 알림
    /// </summary>
    public void Dispose()
    {
        // 재연결 작업 취소 - 백그라운드 재연결 태스크 중지
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        
        // OPC UA 세션 정리
        try
        {
            // 연결된 세션이 있으면 먼저 닫기
            if (_session != null && _session.Connected)
            {
                _session.CloseAsync().GetAwaiter().GetResult();
            }
        }
        catch { } // 정리 중 에러는 무시 (이미 끊어진 세션일 수 있음)
        
        // 세션 및 락 리소스 해제
        _session?.Dispose();
        _connectionLock?.Dispose();
        
        _logger.LogInformation("OpcUaClientAdapter disposed. Disconnected from Kepware OPC UA Server.");
    }
}
