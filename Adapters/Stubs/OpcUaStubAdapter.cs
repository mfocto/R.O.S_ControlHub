using ROS_ControlHub.Adapters.Abstractions;

namespace ROS_ControlHub.Adapters.Stubs;

public class OpcUaStubAdapter : IOpcUaAdapter
{
    public Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct)
    { 
        // TODO : 연결확인용 추후 변경 필요

        IDictionary<string, object> ext = new Dictionary<string, object>
        {
            ["opc.connected"] = true,
            ["opc.conveyor.running"] = true,
            ["opc.conveyor.speed"] = 0.8
        };
        
        return Task.FromResult(ext);
    }
}