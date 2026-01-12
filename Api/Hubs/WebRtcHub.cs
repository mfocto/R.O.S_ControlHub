using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
// using ROS_ControlHub.Infrastructure.Database; // DB 연결 제거
using ROS_ControlHub.Adapters.Abstractions;
// using ROS_ControlHub.Application.Entities; // DB 연결 제거

namespace ROS_ControlHub.Api.Hubs;

/// <summary>
/// unity - web 시그널링 담당
/// </summary>
public class WebRtcHub(
    // AppDbContext context, // DB 연결 제거
    IOpcUaAdapter opcAdapter
    ) : Hub
{
    private static readonly ConcurrentDictionary<string, string> Broadcasters = new();
    private static readonly ConcurrentDictionary<string, string> _viewers = new();
    
    // DB context & Adapter

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
    /// 웹 뷰어에서 제어 명령 전송
    /// </summary>
    public async Task SendControlCommand(string roomId, string command, string value)
    {
        Console.WriteLine($"[Control] roomId: {roomId}, command: {command}, value: {value}");
        
        // DB 로그 저장 제거됨
        
        // 실제 설비 제어 (OPC-UA)
        try
        {
            // value가 JSON 형태이거나 단순 문자열일 수 있음. 프로토콜에 맞게 변환 필요.
            // 여기서는 value를 그대로 payload로 가정하고 전송
            // roomId를 DeviceId로 가정
            var deviceId = roomId; 
            var stateJson = $"{{\"command\": \"{command}\", \"payload\": \"{value}\"}}";
            
            await opcAdapter.WriteStateAsync(deviceId, stateJson);
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Adapter write failed: {ex.Message}");
        }
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // ... (기존 동일)
        // broadcaster가 나간 경우
        foreach (var kv in Broadcasters)
        {
            if (kv.Value == Context.ConnectionId)
            {
                Broadcasters.TryRemove(kv.Key, out _);
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