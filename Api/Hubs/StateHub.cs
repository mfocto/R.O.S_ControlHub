using Microsoft.AspNetCore.SignalR;

namespace ROS_ControlHub.Api.Hubs;


/// <summary>
/// 상태 브로드캐스트 Hub
/// - roomId 그룹 단위로 구독자를 묶어 SystemStateUpdated 이벤트를 송신
/// - Web - Unity 는 이 Hub를 통해서만 상태 전달
/// </summary>
public class StateHub : Hub
{
    /// <summary>
    /// roomId 그룸 참가
    /// </summary>
    /// <param name="roomId"></param>
    public async Task Join(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
    }

    /// <summary>
    /// roomId   
    /// </summary>
    /// <param name="roomId"></param>
    public async Task Leave(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }
}