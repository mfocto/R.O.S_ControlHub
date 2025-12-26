using ROS_ControlHub.Application.State;

namespace ROS_ControlHub.Contracts.Dto;

public record SystemStateDto
{
    public DateTimeOffset timeStamp { get; init; }

    public SystemMode systemMode { get; init; } = SystemMode.Idle;

    public JobPhase jobPhase { get; init; } = JobPhase.None;
    
    public bool emergencyStop { get; init; }
    
    public Dictionary<string, object> extensions { get; init; } = new();

    public static SystemStateDto From(SystemState s)
        => new()
        {
            timeStamp = s.timestamp,
            systemMode = s.systemMode,
            jobPhase = s.jobPhase,
            emergencyStop = s.emergencyStop,
            extensions = s.extensions.ToDictionary(k => k.Key, v => v.Value)
        };
}