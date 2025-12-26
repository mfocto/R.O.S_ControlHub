namespace ROS_ControlHub.Adapters.Abstractions;


/// <summary>
/// OPC UA 어댑터 추상화
/// TODO : 실제 OPC UA Client SDK로 교체
/// </summary>
public interface IOpcUaAdapter
{
    // 설비 상태를 읽어 Extexsions 형태로 반환
    Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct);
}