param(
  # docker_up에서 사용한 컨테이너 이름
  [string]$ContainerName = "app-central-sot"
)

Write-Host "=== Docker Down ==="

# 컨테이너 존재 여부 확인
$existing = docker ps -a --filter "name=$ContainerName" -q

if (-not $existing) {
    Write-Host "No container named '$ContainerName' found."
    return
}

# 실행 중이면 정상 종료(SIGTERM)
Write-Host "Stopping container: $ContainerName"
docker stop $ContainerName | Out-Null

# 컨테이너 제거
Write-Host "Removing container: $ContainerName"
docker rm $ContainerName | Out-Null

Write-Host "Done."
