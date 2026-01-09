namespace ROS_ControlHub.Application.State;

/// <summary>
/// In-memory 저장소
/// </summary>
public class InMemoryStateStore
{
    private SystemState _current = new ();

    /// <summary>
    /// 현재 스냅샷 반환
    /// </summary>
    public SystemState GetSnapshot() => _current;

    public void Update(Func<SystemState, SystemState> updater)
    {
        while (true) {
            var snapshot = _current;
            var updated = updater(snapshot);

            // _current 값과 snapshot 의 값을 비교
            // 둘이 같다면 _updated 로 값 변경
            // 만약 중간에 다른 스레드가 값을 변경 시 교체X
            var original = Interlocked.CompareExchange(ref _current, updated, snapshot);
            
            if (ReferenceEquals(original, snapshot))
                // 값 업데이트 성공시에만 루프 탈출
                break;
        }
    }
}