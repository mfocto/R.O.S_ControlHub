namespace ROS_ControlHub.Adapters.Abstractions;

/// <summary>
/// ROS 어댑터 추상화
/// TODO :
///   - gRPC Unary(Command/Query)
///   - gRPC Streaming(State)
///   - 또는 MQTT 대체
/// </summary>
public interface IRosAdapter
{
    // 상태를 읽어 Extexsions 형태로 반환
    Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct);
}