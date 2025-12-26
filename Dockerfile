# 실행환경
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

#Http 포트
EXPOSE 8080 

# 빌드환경
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 캐시 활용을 위해 csproj 먼저 복사 후 restore
COPY ["ROS_ControlHub.csproj", "./"]
RUN dotnet restore "ROS_ControlHub.csproj"

# 나머지 소스 복사 및 빌드
COPY . .
RUN dotnet build "ROS_ControlHub.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. 게시 (Publish)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ROS_ControlHub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. 최종 이미지 (Final)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ROS_ControlHub.dll"]
