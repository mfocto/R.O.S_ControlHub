namespace ROS_ControlHub.Application.State;

public record SystemState
{
    public DateTimeOffset timestamp { get; init; } = DateTimeOffset.UtcNow;

    public SystemMode systemMode { get; init; } =  SystemMode.Idle;
    
    public JobPhase jobPhase { get; init; } = JobPhase.None;

    public bool emergencyStop { get; init; } = false;
    
    // 이후 object 대신 DTO로 대체 가능
    public IReadOnlyDictionary<string, object> extensions { get; init; } 
        = new Dictionary<string, object>();
}

