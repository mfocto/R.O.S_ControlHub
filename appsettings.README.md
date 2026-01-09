# appsettings.json 설정 가이드

이 문서는 `appsettings.json` 파일의 각 설정 항목에 대한 설명입니다.

## 전체 구조

```json
{
  "Logging": { ... },
  "StatePolling": { ... },
  "OpcUa": { ... },
  "AllowedHosts": "*"
}
```

---

## 1. Logging 섹션

### 작성 이유
- 애플리케이션의 로그 레벨을 제어합니다.
- 개발/프로덕션 환경에 따라 로그 출력량을 조절할 수 있습니다.

### 설정 항목

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",        // 기본 로그 레벨
    "Microsoft.AspNetCore": "Warning", // ASP.NET Core 프레임워크 로그는 Warning 이상만
    "App": "Information"              // 애플리케이션 로그는 Information 이상
  }
}
```

- **Default**: 모든 로거의 기본 로그 레벨
- **Microsoft.AspNetCore**: ASP.NET Core 프레임워크 로그 레벨 (너무 많은 로그 방지)
- **App**: 애플리케이션 커스텀 로그 레벨

---

## 2. StatePolling 섹션

### 작성 이유
- `StatePollingWorker`가 Kepware에서 상태를 읽어오는 주기를 설정합니다.
- 너무 짧으면 Kepware 서버에 부하를 줄 수 있고, 너무 길면 실시간성이 떨어집니다.

### 설정 항목

```json
"StatePolling": {
  "IntervalMs": 500  // 상태 읽기 주기 (밀리초)
}
```

- **IntervalMs**: Kepware에서 상태를 읽어오는 간격 (밀리초)
  - 기본값: 500ms (0.5초)
  - 권장 범위: 100ms ~ 5000ms
  - 값이 작을수록 더 실시간이지만 Kepware 서버 부하 증가

---

## 3. OpcUa 섹션

### 작성 이유
- Kepware OPC UA 서버와의 연결 및 통신에 필요한 모든 설정을 포함합니다.
- 보안, 타임아웃, 재연결, Kepware 태그 구조 등을 설정합니다.

### 설정 항목

#### 3.1 기본 연결 설정

```json
"OpcUa": {
  "EndpointUrl": "opc.tcp://127.0.0.1:49320",
  "SessionName": "ROS_ControlHub_Client"
}
```

- **EndpointUrl**: Kepware OPC UA 서버 주소
  - 형식: `opc.tcp://[IP주소]:[포트]`
  - 기본값: `opc.tcp://127.0.0.1:49320` (로컬호스트, Kepware 기본 포트)
  - 예시: `opc.tcp://192.168.1.100:49320` (원격 서버)

- **SessionName**: OPC UA 세션 이름
  - Kepware 서버에서 이 클라이언트를 식별하는 이름
  - 기본값: "ROS_ControlHub_Client"
  - 여러 인스턴스 실행 시 고유한 이름 사용 권장

#### 3.2 보안 설정

```json
"SecurityMode": "None",
"SecurityPolicy": "None",
"AutoAcceptUntrustedCertificates": true
```

- **SecurityMode**: 보안 모드
  - `"None"`: 암호화 없이 통신 (개발 환경용, 빠른 통신)
  - `"SignAndEncrypt"`: 서명 및 암호화 (프로덕션 환경 권장)
  - 기본값: "None"

- **SecurityPolicy**: 보안 정책 URI
  - `"None"`: 보안 정책 없음 (개발 환경용)
  - `"Basic256Sha256"`: 기본 암호화 정책 (프로덕션 환경 권장)
  - 기본값: "None"

- **AutoAcceptUntrustedCertificates**: 신뢰되지 않은 인증서 자동 수락
  - `true`: 개발 환경 편의를 위해 자동 수락
  - `false`: 프로덕션 환경에서는 false로 설정하고 인증서를 신뢰 목록에 추가
  - 기본값: true

#### 3.3 타임아웃 및 재연결 설정

```json
"ReconnectIntervalMs": 5000,
"ConnectionTimeoutMs": 15000,
"SessionTimeoutMs": 60000
```

- **ReconnectIntervalMs**: 재연결 시도 간격 (밀리초)
  - 연결이 끊어졌을 때 재연결을 시도하는 주기
  - 기본값: 5000ms (5초)
  - 너무 짧으면 Kepware 서버에 부하를 줄 수 있음

- **ConnectionTimeoutMs**: 연결 타임아웃 (밀리초)
  - Kepware 서버에 연결을 시도할 때 최대 대기 시간
  - 기본값: 15000ms (15초)
  - 네트워크가 느린 환경에서는 값 증가 권장

- **SessionTimeoutMs**: 세션 타임아웃 (밀리초)
  - OPC UA 세션이 유지되는 최대 시간
  - 이 시간 동안 통신이 없으면 세션이 자동으로 종료됨
  - 기본값: 60000ms (60초)
  - 주기적으로 통신하는 경우 이 값보다 짧은 간격으로 통신해야 함

#### 3.4 Kepware 태그 구조 설정

```json
"Kepware": {
  "Channel": "Channel1",
  "Device": "Device1",
  "Tags": {
    "Connected": "Connected",
    "Running": "Running",
    "Speed": "Speed",
    "ControlCommand": "ControlCommand"
  }
}
```

- **Channel**: Kepware 채널 이름
  - Kepware 프로젝트에서 정의한 채널 이름
  - 기본값: "Channel1"
  - Node ID 형식: `ns=2;s=Channel1.Device1.TagName`

- **Device**: Kepware 디바이스 이름
  - 특정 PLC/설비를 식별하는 디바이스 이름
  - 기본값: "Device1"
  - Node ID 형식: `ns=2;s=Channel1.Device1.TagName`

- **Tags**: 읽기/쓰기할 태그 이름들
  - **Connected**: 디바이스 연결 상태 태그 (읽기 전용)
    - 기본값: "Connected"
    - 읽기 결과: `result["opc.connected"]`에 저장됨
  
  - **Running**: 컨베이어/설비 실행 상태 태그 (읽기 전용)
    - 기본값: "Running"
    - 읽기 결과: `result["opc.conveyor.running"]`에 저장됨
  
  - **Speed**: 컨베이어/설비 속도 태그 (읽기 전용)
    - 기본값: "Speed"
    - 읽기 결과: `result["opc.conveyor.speed"]`에 저장됨
  
  - **ControlCommand**: 제어 명령 태그 (쓰기 전용)
    - 기본값: "ControlCommand"
    - 쓰기 형식: JSON 문자열 (예: `{"status": "start", "payload": {}}`)

### Kepware Node ID 형식

Kepware는 다음 형식의 Node ID를 사용합니다:
```
ns=2;s=Channel.Device.Tag
```

예시:
- `ns=2;s=Channel1.Device1.Connected`
- `ns=2;s=Channel1.Device1.Running`
- `ns=2;s=Channel1.Device1.Speed`
- `ns=2;s=Channel1.Device1.ControlCommand`

---

## 4. AllowedHosts 섹션

### 작성 이유
- HTTP 요청의 Host 헤더 검증을 제어합니다.
- 보안을 위해 특정 호스트만 허용할 수 있습니다.

### 설정 항목

```json
"AllowedHosts": "*"
```

- **AllowedHosts**: 허용할 호스트 목록
  - `"*"`: 모든 호스트 허용 (개발 환경용)
  - 특정 호스트만 허용: `"localhost;example.com"`
  - 기본값: "*"

---

## 설정 예시

### 개발 환경 (로컬 Kepware)

```json
{
  "OpcUa": {
    "EndpointUrl": "opc.tcp://127.0.0.1:49320",
    "SessionName": "ROS_ControlHub_Dev",
    "SecurityMode": "None",
    "SecurityPolicy": "None",
    "AutoAcceptUntrustedCertificates": true,
    "Kepware": {
      "Channel": "Channel1",
      "Device": "Device1"
    }
  }
}
```

### 프로덕션 환경 (원격 Kepware, 보안 활성화)

```json
{
  "OpcUa": {
    "EndpointUrl": "opc.tcp://192.168.1.100:49320",
    "SessionName": "ROS_ControlHub_Prod",
    "SecurityMode": "SignAndEncrypt",
    "SecurityPolicy": "Basic256Sha256",
    "AutoAcceptUntrustedCertificates": false,
    "ReconnectIntervalMs": 10000,
    "ConnectionTimeoutMs": 30000,
    "SessionTimeoutMs": 120000,
    "Kepware": {
      "Channel": "ProductionChannel",
      "Device": "ProductionDevice"
    }
  }
}
```

---

## 참고사항

1. **설정 변경 후 재시작 필요**: 대부분의 설정은 애플리케이션 시작 시 읽히므로 변경 후 재시작이 필요합니다.

2. **Kepware 태그 이름 확인**: 실제 Kepware 프로젝트에서 정의한 태그 이름과 일치해야 합니다.

3. **보안 설정**: 프로덕션 환경에서는 반드시 보안 모드를 활성화하고 인증서를 관리해야 합니다.

4. **네트워크 환경**: 원격 Kepware 서버에 연결하는 경우 방화벽 설정을 확인하세요.
