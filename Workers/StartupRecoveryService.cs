using Microsoft.EntityFrameworkCore;
using ROS_ControlHub.Adapters.Abstractions;
using ROS_ControlHub.Infrastructure.Database;

namespace ROS_ControlHub.Workers;

public class StartupRecoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpcUaAdapter _opcAdapter;
    private readonly ILogger<StartupRecoveryService> _logger;

    public StartupRecoveryService(
        IServiceProvider serviceProvider,
        IOpcUaAdapter opcAdapter,
        ILogger<StartupRecoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _opcAdapter = opcAdapter;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartupRecoveryService starting...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. DB에서 모든 디바이스의 최신 상태 조회
            // 'DeviceStates' 테이블(ControlStateCurrentEntity)을 조회
            var currentStates = await dbContext.DeviceStates.ToListAsync(cancellationToken);

            if (currentStates.Count == 0)
            {
                _logger.LogInformation("No device states found in DB. Skipping recovery.");
                return;
            }

            _logger.LogInformation("Found {Count} device states. Recovering...", currentStates.Count);

            foreach (var state in currentStates)
            {
                if (string.IsNullOrEmpty(state.StateJson) || state.StateJson == "{}")
                {
                    continue;
                }

                try
                {
                    // DeviceId를 알아야 하는데 ControlStateCurrentEntity에는 DevicePk만 있음
                    // DeviceEntity를 조인해서 가져오거나, 편의상 DevicePk를 문자열로 사용하거나 등 정책 필요
                    // 여기서는 DevicePk를 통해 DeviceId를 조회해야 하나, 
                    // 간단히 DevicePk를 string으로 변환하여 사용 (실제 환경에선 Join 필요)
                    
                    var device = await dbContext.Devices.FindAsync(new object[] { state.DevicePk }, cancellationToken);
                    if (device != null)
                    {
                         await _opcAdapter.WriteStateAsync(device.DeviceId, state.StateJson);
                         _logger.LogInformation("Recovered state for device {DeviceId}", device.DeviceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recover state for DevicePk={DevicePk}", state.DevicePk);
                }
            }
        }
        
        _logger.LogInformation("StartupRecoveryService completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
