using Microsoft.EntityFrameworkCore;
using ROS_ControlHub.Infrastructure.Database;
using ROS_ControlHub.Infrastructure.Repositories;

namespace ROS_ControlHub.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

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
