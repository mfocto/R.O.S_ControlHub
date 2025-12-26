param(
  # Docker 이미지 이름
  [string]$ImageName = "app-central-sot",

  # 로컬 포트
  [int]$HostPort = 8080,

  # 컨테이너 내부 포트( Dockerfile에서 ASPNETCORE_URLS가 8080 )
  [int]$ContainerPort = 8080
)

$ErrorActionPreference = "Stop"

Write-Host "=== Docker Build ==="
# - 현재 디렉토리(Dockerfile 위치)를 build context로 이미지 생성
docker build -t $ImageName .

Write-Host "=== Docker Run ==="
# - 동일 이름 컨테이너가 있으면 제거 (재실행 용이)
$existing = docker ps -a --filter "name=$ImageName" -q
if ($existing) { docker rm -f $ImageName | Out-Null }

# - 컨테이너 실행 및 포트 매핑
docker run -d --name $ImageName -p "$HostPort`:$ContainerPort" $ImageName

Write-Host "=== Done ==="
Write-Host "Health:  http://localhost:$HostPort/health"
Write-Host "State:   http://localhost:$HostPort/state"
Write-Host "SignalR: http://localhost:$HostPort/hubs/state"
