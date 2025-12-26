param(
  # 빌드 대상 .csproj
  [string]$ProjectPath = ".\App.csproj",

  # Release / Debug
  [string]$Configuration = "Release",

  # publish 결과 경로 (Docker 스크립트에서 COPY 대상으로 사용)
  [string]$PublishDir = ".\publish"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Restore (NuGet packages incl. Grpc.Tools) ==="
# - 패키지 복원: Grpc.Tools 포함
dotnet restore $ProjectPath

Write-Host "=== Build (gRPC proto codegen happens during build) ==="
# - 이 단계에서 control.proto → C# 생성 코드가 obj/ 아래 생성됨
dotnet build $ProjectPath -c $Configuration

Write-Host "=== Publish (deployable output) ==="
# - 기존 publish 제거(충돌 방지)
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

# - 실행에 필요한 파일만 모아서 publish 디렉토리에 출력
dotnet publish $ProjectPath -c $Configuration -o $PublishDir

Write-Host "=== Done ==="
Write-Host "PublishDir: $PublishDir"
Write-Host "Note: Proto codegen output is under obj/ (generated during build)."
