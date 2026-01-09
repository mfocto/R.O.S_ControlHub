using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ROS_ControlHub.Application.Entities;

[Table("devices")]
public class DeviceEntity
{
    [Key]
    [Column("device_pk")]
    public long DevicePk { get; set; }

    [Column("room_pk")]
    public long RoomPk { get; set; }

    [Column("device_id")]
    [Required]
    public string DeviceId { get; set; } = string.Empty; // robot-1

    [Column("device_type")]
    public string DeviceType { get; set; } = string.Empty; // robot

    // PostgreSQL JSONB type
    [Column("device_meta", TypeName = "jsonb")]
    public string DeviceMeta { get; set; } = "{}";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // 관계설정
    [ForeignKey(nameof(RoomPk))]
    public RoomsEntity? Room { get; set; }
    
    public ICollection<SystemLogEntity> SystemLogs { get; set; } = new List<SystemLogEntity>();
    public ICollection<ControlStateEventEntity> Events { get; set; } = new List<ControlStateEventEntity>();
    public ICollection<ControlApplyStatusEntity> Statuses { get; set; } = new List<ControlApplyStatusEntity>();
    public ICollection<DeviceActualStateHistoryEntity> StatusHistory { get; set; } = new List<DeviceActualStateHistoryEntity>();
    
    public ControlStateCurrentEntity? CurrentState { get; set; }
}
