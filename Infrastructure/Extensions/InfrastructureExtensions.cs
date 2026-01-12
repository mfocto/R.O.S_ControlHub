using Microsoft.EntityFrameworkCore;
using ROS_ControlHub.Infrastructure.Database;
using ROS_ControlHub.Infrastructure.Repositories;

namespace ROS_ControlHub.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 연결 문자열을 설정 파일에서 읽기
        // appsettings.json의 "ConnectionStrings:DefaultConnection"에서 읽음
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // 연결 문자열이 없으면 null이 될 수 있으므로, 기본값 제공 또는 예외 처리
        if (string.IsNullOrEmpty(connectionString))
        {
            // 연결 문자열이 없을 때의 처리 방법:
            // 1. 기본값 사용 (개발 환경)
            // 2. 예외 발생 (프로덕션 환경에서는 필수)
            connectionString = "Host=localhost;Database=ros_controlhub;Username=postgres;Password=codelab";
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        // factory가 필요한 경우
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Repositories 등록
        services.AddScoped<IDeviceRepository, DeviceRepository>();

        return services;
    }
}
