using Microsoft.EntityFrameworkCore;
using ROS_ControlHub.Application.Entities;
using ROS_ControlHub.Infrastructure.Database;

namespace ROS_ControlHub.Infrastructure.Repositories;

public interface IDeviceRepository
{
    Task<DeviceEntity?> GetDeviceAsync(string deviceId);
    Task UpdateStateAsync(long devicePk, string newStateJson);
}

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;

    public DeviceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceEntity?> GetDeviceAsync(string deviceId)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
    }

    public async Task UpdateStateAsync(long devicePk, string newStateJson)
    {
        // 트랜잭션은 SaveChangesAsync가 호출될 때 자동으로 하나의 단위로 처리됩니다.
        // 만약 더 복잡한 트랜잭션이 필요하면 _context.Database.BeginTransactionAsync() 사용
        
        var state = await _context.DeviceStates.FindAsync(devicePk);
        if (state == null)
        {
            state = new ControlStateCurrentEntity
            {
                DevicePk = devicePk,
                Version = 0
            };
            _context.DeviceStates.Add(state);
        }

        state.StateJson = newStateJson;
        state.Version++;
        state.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
    }
}
