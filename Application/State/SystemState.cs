namespace ROS_ControlHub.Application.State;

public record SystemState
{
    public DateTimeOffset timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    public string deviceName { get; init; } = "";
    
    public string deviceStatus { get; init; } = "Idle";
    // 이후 object 대신 DTO로 대체 가능
    public IReadOnlyDictionary<string, object> extensions { get; init; } 
        = new Dictionary<string, object>();
}

