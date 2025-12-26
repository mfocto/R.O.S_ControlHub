# R.O.S Control Hub

![.NET 10](https://img.shields.io/badge/.NET-10.0-purple?style=flat-square&logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Supported-blue?style=flat-square&logo=docker)

로봇, AGV, 설비를 통합 제어하기 위한 **ASP.NET Core 기반 중앙 서버** 프로젝트입니다.
시스템 상태를 통합 관리하며 Web 관제, Unity 디지털 트윈, ROS 로봇, PLC 설비 간의 통신을 중계합니다.

---

## 프로젝트 목적

* 로봇 및 설비의 상태 정보를 단일 지점으로 통합
* 작업(Job) 흐름을 서버에서 제어
* 여러 시스템이 **직접 연결되지 않고 서버를 통해서만 통신**하도록 구성

---

## 주요 기능

* **System State**: 전역 시스템 상태 실시간 관리 및 동기화
* **Job Control**: 작업 단계 관리 및 프로세스 제어
* **Real-time Data**: SignalR을 통한 실시간 데이터 브로드캐스트
* **Interface**: gRPC(로봇 제어), OPC UA(설비 연동), REST API 지원
* **Digital Twin**: Unity 디지털 트윈 연동

---

## 사용 기술

| 구분 | 기술 스택 |
| :--- | :--- |
| **Framework** | .NET 10 (ASP.NET Core) |
| **Communication** | SignalR, gRPC, REST API |
| **Protocol** | OPC UA |
| **Infrastructure** | Docker, BackgroundService |

---

## 프로젝트 구조

- **Api/**: Controller, SignalR Hub 
- **Application/**: SystemState, 핵심 비즈니스 로직
- **Adapters/**: ROS, OPC UA 연동 코드 
- **Workers/**: BackgroundService
- **Contracts/**: DTO, gRPC proto 

---

## 실행 방법 (Local)

### 1. 빌드 및 실행
```bash
dotnet restore
dotnet build
dotnet run
```

### 2. 엔드포인트 확인
* **Health Check**: `GET http://localhost:****/health`
* **System State**: `GET http://localhost:****/state`

> **참고**: 포트 번호는 실행 시 콘솔 로그(Terminal)에 출력되는 주소를 확인하세요.

---

## Docker 실행 가이드

1. **Publish 파일 생성**
```
dotnet publish -c Release -o publish
```

2. **컨테이너 관리 (스크립트 활용)**
* **실행**: `./docker_up.sh`
* **종료**: `./docker_down.sh`

---

## 시스템 구성 원칙

1. **서버 중심 통신**: 모든 클라이언트(Web, Unity, ROS, 설비)는 서버를 통해서만 데이터를 주고받습니다.
2. **실시간성 유지**: 시스템의 모든 상태 변화는 SignalR을 통해 지연 없이 브로드캐스트됩니다.
3. **이력 관리**: 주요 작업 이력과 이벤트는 서버에서 통합 관리하여 데이터 일관성을 보장합니다.
