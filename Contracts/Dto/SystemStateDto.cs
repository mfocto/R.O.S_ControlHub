using ROS_ControlHub.Application.State;

namespace ROS_ControlHub.Contracts.Dto;

public record SystemStateDto
{
    public DateTimeOffset timeStamp { get; init; }

    public string deviceName { get; init; } = "";
    
    public string deviceStatus { get; init; } = "";
    
    public Dictionary<string, object> extensions { get; init; } = new();

    public static SystemStateDto From(SystemState s)
        => new()
        {
            timeStamp = s.timestamp,
            deviceName = s.deviceName,
            deviceStatus = s.deviceStatus,
            extensions = s.extensions.ToDictionary(k => k.Key, v => v.Value)
        };
}