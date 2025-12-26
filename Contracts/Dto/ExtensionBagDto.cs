namespace ROS_ControlHub.Contracts.Dto;

public record ExtensionBagDto
{
    /// <summary>
    /// 확장슬롯 데이터
    /// ex :
    ///     - ros.connected
    ///     - opc.conveyor.speed
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}