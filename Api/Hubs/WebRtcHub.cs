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
        Console.WriteLine($"[Relay] {Context.ConnectionId} {type} {payload}");
        if (targetId != null)
        {
            return Clients.Client(targetId).SendAsync("Signal", type, payload, Context.ConnectionId);
        }
        
        return Clients.Group(roomId).SendAsync("Signal", type, payload, Context.ConnectionId);
    }

    public Task CheckBroadcaster(string roomId)
    {
        int broadCasterCount = Broadcasters[roomId].Length;

        if (broadCasterCount > 1)
        {
            return Clients.Caller.SendAsync("BroadcasterOnline", Context.ConnectionId);
        }

        return Clients.Caller.SendAsync("BroadcasterNotOnline", Context.ConnectionId);
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