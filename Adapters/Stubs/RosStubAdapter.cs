using ROS_ControlHub.Adapters.Abstractions;

namespace ROS_ControlHub.Adapters.Stubs;

public class RosStubAdapter : IRosAdapter
{
    public int _tick;
    
    public Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct)
    {
        // TODO : 연결확인용 추후 변경 필요
        _tick++;

        IDictionary<string, object> ext = new Dictionary<string, object>
        {
            ["ros.connected"] = true,
            ["ros.tick"] =  _tick,
            ["ros.robot.status"] = "OK",
            ["ros.robos.pose"] = new { x = 1.0, y = 2.0, z = 0.5}
        };

        return Task.FromResult(ext);
    }
}