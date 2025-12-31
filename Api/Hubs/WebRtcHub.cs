using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ROS_ControlHub.Api.Hubs;

/// <summary>
/// unity - web 시그널링 담당
/// </summary>
public class WebRtcHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> Broadcasters = new();
    private static readonly ConcurrentDictionary<string, string> _viewers = new();
    
    // web
    public async Task JoinViewers(string roomId)
    {
        _viewers[Context.ConnectionId] = roomId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}:viewers");
        
        // 이미 broadcaster가 있으면, 새로 열린 창에도 알려줌
        if (Broadcasters.TryGetValue(roomId, out var broadcasterId))
        {
            await Clients.Caller.SendAsync("BroadcasterOnline", broadcasterId);
            
            // broadcaster 에게 viewerId 전달
            await Clients.Client(broadcasterId).SendAsync("ViewerJoined", Context.ConnectionId);
        }
    }
    
    // unity
    public async Task RegisterBroadcaster(string roomId)
    {
        Broadcasters[roomId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}:broadcaster");
        
        await Clients.Group($"room:{roomId}:viewers").SendAsync("BroadcasterOnline", Context.ConnectionId);
        
    }
    
    // Offer / Answer / ICE relay
    public Task Relay(string roomId, string type, string payload, string? targetId = null)
    {
        Console.WriteLine($"[Relay] type : {type}, target : {targetId}");
        if (targetId != null)
        {
            return Clients.Client(targetId).SendAsync("Signal", type, payload, Context.ConnectionId);
        }
        
        return Clients.Group($"room:{roomId}:viewers").SendAsync("Signal", type, payload, Context.ConnectionId);
    }

    public Task CheckBroadcaster(string roomId)
    {
        if (Broadcasters.TryGetValue(roomId, out var broadcasterId))
        {
            return Clients.Caller.SendAsync("BroadcasterOnline", broadcasterId);
        }

        return Clients.Caller.SendAsync("BroadcasterNotOnline");
    }
    
    /// <summary>
    /// 웹 뷰어에서 Unity로 제어 명령 전송
    /// </summary>
    /// <param name="roomId">방 ID</param>
    /// <param name="command">명령 타입 (예: "move", "rotate", "emergency")</param>
    /// <param name="value">명령 값 (예: "forward", "left", "stop")</param>
    public Task SendControlCommand(string roomId, string command, string value)
    {
        Console.WriteLine($"[Control] roomId: {roomId}, command: {command}, value: {value}");
        
        if (Broadcasters.TryGetValue(roomId, out var broadcasterId))
        {
            // Unity broadcaster에게 제어 명령 전송
            return Clients.Client(broadcasterId).SendAsync("ControlCommand", command, value);
        }
        
        Console.WriteLine($"[Control] Broadcaster not found for room: {roomId}");
        return Task.CompletedTask;
    }
    
    //
    // public async Task BroadcasterReady(string roomId)
    // {
    //     await Clients.Group(roomId).SendAsync("BroadcasterReady");
    // }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // broadcaster가 나간 경우
        foreach (var kv in Broadcasters)
        {
            if (kv.Value == Context.ConnectionId)
            {
                Broadcasters.TryRemove(kv.Key, out _);
                // viewers에게 오프라인 알림을 원하면 여기에 추가
                break;
            }
        }

        // viewer가 나간 경우
        if (_viewers.TryRemove(Context.ConnectionId, out var roomId))
        {
            if (Broadcasters.TryGetValue(roomId, out var broadcasterId))
            {
                await Clients.Client(broadcasterId)
                    .SendAsync("ViewerLeft", Context.ConnectionId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}