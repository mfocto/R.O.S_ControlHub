using Microsoft.AspNetCore.SignalR;
using ROS_ControlHub.Adapters.Abstractions;
using ROS_ControlHub.Api.Hubs;
using ROS_ControlHub.Application.State;
using ROS_ControlHub.Contracts.Dto;

namespace ROS_ControlHub.Workers;

public class StatePollingWorker(
    // IRosAdapter ros, // 제거
    IOpcUaAdapter opc,
    InMemoryStateStore store,
    IHubContext<StateHub> hub,
    ILogger<StatePollingWorker> logger,
    IConfiguration config
    ) : BackgroundService
{
    // private readonly IRosAdapter _ros = ros;
    private readonly IOpcUaAdapter _opc = opc;
    private readonly InMemoryStateStore _store = store;
    private readonly IHubContext<StateHub> _hub = hub;
    private readonly ILogger<StatePollingWorker> _logger = logger;

    private readonly int _intervalMs = config.GetValue("StatePolling:IntervalMs", 500);
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("StatePollingWorker started. IntervalMs={IntervalMs}", _intervalMs);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // var rosExt = await _ros.ReadStateAsync(ct);
                var opcExt = await _opc.ReadStateAsync(ct);
                
                _store.Update(prev =>
                {
                    var merged = prev.extensions.ToDictionary(k => k.Key, v => v.Value);
                    
                    // foreach (var kv in rosExt) merged[kv.Key] = kv.Value;
                    foreach (var kv in opcExt) merged[kv.Key] = kv.Value;

                    var deviceName = Convert.ToString(merged["deviceName"]);
                    var deviceStatus = Convert.ToString(merged["deviceStatus"]);

                    return prev with
                    {
                        timestamp = DateTimeOffset.UtcNow,
                        deviceName = deviceName,
                        deviceStatus = deviceStatus,
                        extensions = merged
                    };
                });
                
                var snapshot = _store.GetSnapshot();
                var dto = SystemStateDto.From(snapshot);

                await _hub.Clients
                    .Group("default")
                    .SendAsync("SystemStateUpdated", dto, ct);
                
                _logger.LogDebug("SystemStateUpdated broadcasted. device = {deviceName}, status={deviceStatus}", dto.deviceName, dto.deviceStatus);
                
                await Task.Delay(_intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                // 정상 종료 흐름
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StatePollingWorker loop error");
                // 장애 시에도 워커가 죽지 않도록 약간의 backoff
                await Task.Delay(Math.Min(_intervalMs * 2, 2000), ct);
            }
        }   
        _logger.LogInformation("StatePollingWorker stopped.");
    }
    
    
}