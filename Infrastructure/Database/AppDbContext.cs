using Microsoft.EntityFrameworkCore;
using ROS_ControlHub.Application.Entities;

namespace ROS_ControlHub.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DeviceEntity> Devices { get; set; }
    public DbSet<RoomsEntity> Rooms { get; set; } // 추가
    public DbSet<ControlStateCurrentEntity> DeviceStates { get; set; }
    public DbSet<SystemLogEntity> Logs { get; set; }
    public DbSet<ControlApplyStatusEntity> ApplyStatuses { get; set; }
    public DbSet<ControlStateEventEntity> Events { get; set; }
    public DbSet<DeviceActualStateHistoryEntity> DeviceHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 관계 설정

        // Device
        modelBuilder.Entity<RoomsEntity>()
            .HasMany(r => r.Devices)
            .WithOne(d => d.Room)
            .HasForeignKey(d => d.RoomPk)
            .OnDelete(DeleteBehavior.Cascade); // 부모(Room) 삭제 시 자식(Devices)도 삭제될지 여부 설정

        // SystemLogs
        modelBuilder.Entity<RoomsEntity>()
            .HasMany(r => r.SystemLogs) 
            .WithOne(s => s.Room)
            .HasForeignKey(s => s.RoomPk)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DeviceEntity>() 
            .HasMany(d => d.SystemLogs)
            .WithOne(d => d.Device)
            .HasForeignKey(d => d.DevicePk)
            .OnDelete(DeleteBehavior.Cascade);
        
        // current state
        modelBuilder.Entity<DeviceEntity>()
            .HasOne(d => d.CurrentState)
            .WithOne(c => c.Device)
            .HasForeignKey<ControlStateCurrentEntity>(c => c.DevicePk);

        // apply state
        modelBuilder.Entity<DeviceEntity>()
            .HasMany(d => d.Statuses)
            .WithOne(c=> c.Device)
            .OnDelete(DeleteBehavior.Cascade);

        // event
        modelBuilder.Entity<DeviceEntity>()
            .HasMany(d => d.Events)
            .WithOne(c=> c.Device)
            .OnDelete(DeleteBehavior.Cascade);

        // device state
        modelBuilder.Entity<DeviceEntity>()
            .HasMany(d => d.StatusHistory)
            .WithOne(c => c.Device)
            .OnDelete(DeleteBehavior.Cascade);
        
    }
}
