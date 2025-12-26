namespace ROS_ControlHub.Application.State;


/// <summary>
/// Adapter 결과(extentions) -> SystemState 정규화 매퍼
/// TODO : 추후 추가 예정
/// </summary>
public class StateMapper
{
    public static IReadOnlyDictionary<string, object> MergeExtensions(
        IReadOnlyDictionary<string, object> current,
        IReadOnlyDictionary<string, object> incoming
        )
    {
        var merged = current.ToDictionary(k => k.Key, v => v.Value);

        foreach (var kv in incoming)
        {
            merged[kv.Key] =  kv.Value;
        }

        return merged;
    }
}