# 프로젝트 분석 가이드

이 문서는 R.O.S Control Hub 프로젝트를 처음 접하는 개발자가 코드베이스를 체계적으로 이해하기 위한 분석 순서를 제공합니다.

---

## 📋 목차

1. [프로젝트 개요 파악](#1-프로젝트-개요-파악)
2. [설정 파일 확인](#2-설정-파일-확인)
3. [진입점 분석](#3-진입점-분석)
4. [아키텍처 구조 이해](#4-아키텍처-구조-이해)
5. [주요 컴포넌트 분석](#5-주요-컴포넌트-분석)
6. [데이터 흐름 추적](#6-데이터-흐름-추적)
7. [통신 프로토콜 이해](#7-통신-프로토콜-이해)
8. [데이터베이스 구조 파악](#8-데이터베이스-구조-파악)

---

## 1. 프로젝트 개요 파악

### 1.1 README.md 읽기
**파일 위치**: `README.md`

**분석 목적**:
- 프로젝트의 목적과 주요 기능 이해
- 사용 기술 스택 확인
- 프로젝트 구조 개요 파악

**확인 사항**:
- 프로젝트가 해결하려는 문제는 무엇인가?
- 주요 기능은 무엇인가?
- 어떤 기술을 사용하는가?

### 1.2 프로젝트 파일 구조 확인
**명령어**: `tree /F` 또는 IDE의 프로젝트 탐색기

**확인 사항**:
- 주요 디렉토리 구조
- 각 폴더의 역할
- 파일 명명 규칙

**주요 디렉토리**:
```
├── Adapters/          # 외부 시스템 연동 (OPC UA, ROS)
├── Api/               # REST API, SignalR Hub
├── Application/       # 비즈니스 로직, 엔티티
├── Contracts/         # DTO, gRPC 정의
├── Infrastructure/    # 데이터베이스, 리포지토리
├── Services/          # gRPC 서비스
└── Workers/           # 백그라운드 작업
```

---

## 2. 설정 파일 확인

### 2.1 appsettings.json 분석
**파일 위치**: `appsettings.json`

**분석 목적**:
- 애플리케이션 설정 값 확인
- 외부 시스템 연결 정보 파악
- 동작 주기 및 타임아웃 설정 확인

**확인 사항**:
- OPC UA 서버 주소 및 포트
- Kepware 채널/디바이스 설정
- 상태 폴링 간격
- 데이터베이스 연결 문자열 (있는 경우)

**참고 문서**: `appsettings.README.md` (상세 설정 설명)

### 2.2 프로젝트 파일 확인
**파일 위치**: `ROS_ControlHub.csproj`

**분석 목적**:
- 사용 중인 NuGet 패키지 확인
- .NET 버전 확인
- 프로젝트 타입 확인

**확인 사항**:
- 주요 의존성 패키지 (gRPC, OPC UA, Entity Framework 등)
- .NET 버전 (현재: .NET 10.0)
- 프로젝트 타입 (Web Application)

---

## 3. 진입점 분석

### 3.1 Program.cs 분석
**파일 위치**: `Program.cs`

**분석 목적**:
- 애플리케이션 초기화 과정 이해
- 서비스 등록 순서 확인
- 미들웨어 파이프라인 구성 파악

**분석 순서**:

1. **서비스 등록** (`builder.Services.Add...`)
   - 컨트롤러, SignalR, gRPC 등록
   - 인프라 설정 (데이터베이스 등)
   - 상태 저장소 등록
   - 어댑터 등록 (OPC UA, ROS)
   - 백그라운드 서비스 등록

2. **미들웨어 파이프라인** (`app.Use...`, `app.Map...`)
   - 정적 파일 서빙
   - 컨트롤러 라우팅
   - SignalR Hub 매핑
   - gRPC 서비스 매핑

**확인 사항**:
- 어떤 서비스가 등록되어 있는가?
- 어떤 어댑터가 사용되는가? (Stub vs Real)
- 어떤 백그라운드 작업이 실행되는가?

---

## 4. 아키텍처 구조 이해

### 4.1 계층 구조 파악
**분석 목적**:
- 계층별 책임 이해
- 의존성 방향 확인

**계층 구조**:
```
┌─────────────────────────────────┐
│   Api/ (Presentation Layer)      │  ← REST API, SignalR Hub
├─────────────────────────────────┤
│   Application/ (Domain Layer)   │  ← 비즈니스 로직, 엔티티
├─────────────────────────────────┤
│   Infrastructure/ (Data Layer)   │  ← 데이터베이스, 리포지토리
├─────────────────────────────────┤
│   Adapters/ (Integration Layer)  │  ← 외부 시스템 연동
└─────────────────────────────────┘
```

### 4.2 디렉토리별 역할 이해

#### Adapters/
- **Abstractions/**: 인터페이스 정의
  - `IOpcUaAdapter.cs`: OPC UA 연동 인터페이스
  - `IRosAdapter.cs`: ROS 연동 인터페이스 (현재 미사용)
  
- **Real/**: 실제 구현체
  - `OpcUaClientAdapter.cs`: Kepware OPC UA 클라이언트
  
- **Stubs/**: 테스트용 스텁
  - `OpcUaStubAdapter.cs`: 테스트용 OPC UA 스텁

#### Application/
- **Entities/**: 도메인 엔티티
  - 데이터베이스 테이블과 매핑되는 엔티티 클래스
  
- **State/**: 상태 관리
  - `SystemState.cs`: 시스템 상태 모델
  - `InMemoryStateStore.cs`: 메모리 기반 상태 저장소

#### Api/
- **Controllers/**: REST API 컨트롤러
  - `StateController.cs`: 상태 조회 API
  
- **Hubs/**: SignalR Hub
  - `StateHub.cs`: 상태 브로드캐스트
  - `WebRtcHub.cs`: WebRTC 시그널링

#### Infrastructure/
- **Database/**: 데이터베이스 컨텍스트
  - `AppDbContext.cs`: Entity Framework 컨텍스트
  
- **Repositories/**: 데이터 접근 계층
  - `DeviceRepository.cs`: 디바이스 데이터 접근

---

## 5. 주요 컴포넌트 분석

### 5.1 상태 관리 시스템
**분석 순서**:

1. **SystemState.cs** (Application/State/)
   - 상태 모델 구조 확인
   - 어떤 정보가 저장되는가?

2. **InMemoryStateStore.cs** (Application/State/)
   - 상태 저장 방식 이해
   - Thread-safe 업데이트 메커니즘 확인

3. **StatePollingWorker.cs** (Workers/)
   - 상태 수집 주기 확인
   - 어댑터에서 상태 읽기 과정
   - SignalR 브로드캐스트 과정

**분석 포인트**:
- 상태가 어떻게 업데이트되는가?
- 누가 상태를 읽고 쓰는가?
- 상태 변경이 어떻게 전파되는가?

### 5.2 어댑터 패턴 분석
**분석 순서**:

1. **인터페이스 확인** (Adapters/Abstractions/)
   - `IOpcUaAdapter.cs`: 어떤 메서드가 정의되어 있는가?
   - `IRosAdapter.cs`: ROS 연동 인터페이스 (현재 미사용)

2. **구현체 분석** (Adapters/Real/)
   - `OpcUaClientAdapter.cs`:
     - 연결 설정 방법
     - 상태 읽기 로직
     - 제어 명령 쓰기 로직
     - 재연결 메커니즘

**분석 포인트**:
- 어댑터 패턴의 장점은 무엇인가?
- 실제 구현체와 스텁의 차이는?
- 어떻게 교체할 수 있는가?

### 5.3 통신 채널 분석

#### SignalR Hub
**분석 순서**:

1. **StateHub.cs** (Api/Hubs/)
   - 그룹 관리 방법
   - 브로드캐스트 메서드

2. **WebRtcHub.cs** (Api/Hubs/)
   - WebRTC 시그널링 흐름
   - 제어 명령 전송 로직

#### gRPC 서비스
**분석 순서**:

1. **control.proto** (Contracts/Grpc/)
   - 서비스 정의 확인
   - 메시지 타입 확인

2. **GrpcControlService.cs** (Services/)
   - 각 RPC 메서드 구현
   - 어댑터 호출 방식

#### REST API
**분석 순서**:

1. **StateController.cs** (Api/Controllers/)
   - 엔드포인트 확인
   - 상태 조회 로직

### 5.4 백그라운드 작업 분석
**분석 순서**:

1. **StatePollingWorker.cs** (Workers/)
   - 폴링 주기 확인
   - 상태 수집 및 브로드캐스트 흐름

2. **StartupRecoveryService.cs** (Workers/)
   - 시작 시 복구 로직
   - 데이터베이스에서 상태 복원

---

## 6. 데이터 흐름 추적

### 6.1 상태 수집 흐름
**흐름도**:
```
Kepware OPC UA Server
    ↓
OpcUaClientAdapter.ReadStateAsync()
    ↓
StatePollingWorker.ExecuteAsync()
    ↓
InMemoryStateStore.Update()
    ↓
SignalR StateHub.Broadcast()
    ↓
Web/Unity 클라이언트
```

**분석 방법**:
1. `StatePollingWorker.cs`의 `ExecuteAsync()` 메서드부터 시작
2. `OpcUaClientAdapter.ReadStateAsync()` 호출 추적
3. `InMemoryStateStore.Update()` 호출 확인
4. `StateHub` 브로드캐스트 확인

### 6.2 제어 명령 흐름
**흐름도**:
```
Web/Unity 클라이언트
    ↓
WebRtcHub.SendControlCommand() 또는
GrpcControlService.SetDeviceState()
    ↓
OpcUaClientAdapter.WriteStateAsync()
    ↓
Kepware OPC UA Server
```

**분석 방법**:
1. `WebRtcHub.cs`의 `SendControlCommand()` 메서드 확인
2. `GrpcControlService.cs`의 `SetDeviceState()` 메서드 확인
3. `OpcUaClientAdapter.WriteStateAsync()` 호출 추적

### 6.3 시작 시 복구 흐름
**흐름도**:
```
애플리케이션 시작
    ↓
StartupRecoveryService.StartAsync()
    ↓
AppDbContext에서 최신 상태 조회
    ↓
OpcUaClientAdapter.WriteStateAsync()
    ↓
Kepware OPC UA Server에 상태 복원
```

**분석 방법**:
1. `StartupRecoveryService.cs`의 `StartAsync()` 확인
2. 데이터베이스 쿼리 로직 확인
3. 어댑터를 통한 상태 복원 확인

---

## 7. 통신 프로토콜 이해

### 7.1 OPC UA 프로토콜
**분석 파일**:
- `OpcUaClientAdapter.cs`
- `appsettings.json`의 OpcUa 섹션

**확인 사항**:
- Node ID 형식: `ns=2;s=Channel.Device.Tag`
- 읽기/쓰기 메서드
- 연결 및 재연결 메커니즘

### 7.2 SignalR 프로토콜
**분석 파일**:
- `StateHub.cs`
- `WebRtcHub.cs`

**확인 사항**:
- 그룹 관리 방법
- 브로드캐스트 메서드
- 클라이언트 연결 관리

### 7.3 gRPC 프로토콜
**분석 파일**:
- `control.proto`
- `GrpcControlService.cs`

**확인 사항**:
- 서비스 정의
- 메시지 타입
- RPC 메서드 구현

---

## 8. 데이터베이스 구조 파악

### 8.1 엔티티 분석
**분석 순서**:

1. **엔티티 파일 확인** (Application/Entities/)
   - `DeviceEntity.cs`: 디바이스 정보
   - `RoomsEntity.cs`: 방/구역 정보
   - `ControlStateCurrentEntity.cs`: 현재 상태
   - `ControlStateEventEntity.cs`: 상태 이벤트
   - `ControlApplyStatusEntity.cs`: 제어 적용 상태
   - `DeviceActualStateHistoryEntity.cs`: 상태 이력
   - `SystemLogEntity.cs`: 시스템 로그

2. **관계 확인**:
   - Room ↔ Device (1:N)
   - Device ↔ CurrentState (1:1)
   - Device ↔ Events (1:N)
   - Device ↔ Statuses (1:N)
   - Device ↔ StatusHistory (1:N)

### 8.2 데이터베이스 컨텍스트 분석
**파일 위치**: `Infrastructure/Database/AppDbContext.cs`

**확인 사항**:
- DbSet 정의
- 관계 설정 (OnModelCreating)
- 삭제 동작 (Cascade 등)

### 8.3 DDL 확인 (있는 경우)
**파일 위치**: `DDL.sql`

**확인 사항**:
- 테이블 구조
- 인덱스
- 제약 조건

---

## 📝 분석 체크리스트

프로젝트 분석을 완료했는지 확인하기 위한 체크리스트:

### 기본 이해
- [ ] 프로젝트의 목적과 주요 기능을 이해했는가?
- [ ] 프로젝트 구조와 각 디렉토리의 역할을 이해했는가?
- [ ] 사용 기술 스택을 파악했는가?

### 설정 및 초기화
- [ ] appsettings.json의 모든 설정을 이해했는가?
- [ ] Program.cs의 서비스 등록 순서를 이해했는가?
- [ ] 미들웨어 파이프라인을 이해했는가?

### 핵심 컴포넌트
- [ ] 상태 관리 시스템의 동작 방식을 이해했는가?
- [ ] 어댑터 패턴의 구현을 이해했는가?
- [ ] 각 통신 채널(SignalR, gRPC, REST)의 역할을 이해했는가?

### 데이터 흐름
- [ ] 상태 수집 흐름을 추적할 수 있는가?
- [ ] 제어 명령 흐름을 추적할 수 있는가?
- [ ] 시작 시 복구 흐름을 이해했는가?

### 데이터베이스
- [ ] 엔티티 구조와 관계를 이해했는가?
- [ ] 데이터베이스 컨텍스트 설정을 이해했는가?

---

## 🔍 심화 분석 항목

기본 분석을 완료한 후 다음 항목들을 심화 분석하세요:

### 1. 에러 처리 및 로깅
- 예외 처리 메커니즘
- 로그 레벨 및 로깅 전략
- 재연결 로직의 안정성

### 2. 성능 최적화
- 상태 폴링 주기의 적절성
- 메모리 사용량
- 동시성 처리 (Thread-safety)

### 3. 보안
- OPC UA 보안 설정
- 인증서 관리
- 네트워크 보안

### 4. 확장성
- 새로운 어댑터 추가 방법
- 새로운 통신 채널 추가 방법
- 새로운 엔티티 추가 방법

---

## 💡 분석 팁

1. **디버거 활용**: 실제로 코드를 실행하면서 데이터 흐름을 추적하세요.

2. **의존성 그래프 그리기**: 각 컴포넌트 간의 의존성을 시각화하세요.

3. **로그 확인**: 애플리케이션 실행 시 로그를 확인하여 실제 동작을 관찰하세요.

4. **단위 테스트 확인**: 있다면 테스트 코드를 통해 각 컴포넌트의 동작을 이해하세요.

5. **문서 참조**: README, 주석, 설정 가이드 등을 적극 활용하세요.

---

## 📚 참고 문서

- `README.md`: 프로젝트 개요
- `appsettings.README.md`: 설정 파일 상세 설명
- `DDL.sql`: 데이터베이스 스키마 (있는 경우)

---

## 🎯 다음 단계

프로젝트 분석을 완료한 후:

1. **로컬 환경 구축**: 개발 환경을 설정하고 애플리케이션을 실행해보세요.

2. **기능 테스트**: 각 기능을 테스트하여 실제 동작을 확인하세요.

3. **코드 수정**: 작은 기능부터 시작하여 코드 수정 경험을 쌓으세요.

4. **문서화**: 이해한 내용을 문서로 정리하여 팀과 공유하세요.
