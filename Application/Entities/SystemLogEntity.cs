using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ROS_ControlHub.Application.Entities;

[Table("system_logs")]
public class SystemLogEntity
{
    [Key]
    [Column("log_pk")]
    public long LogPk { get; set; }

    [Column("room_pk")]
    public long? RoomPk { get; set; }

    [Column("device_pk")]
    public long? DevicePk { get; set; }

    [Column("component")]
    public string Component { get; set; } = string.Empty;

    [Column("severity")]
    public string Severity { get; set; } = "INFO";
    
    [Column("event_type")]
    public string EventType { get; set; } = "GENERAL";

    [Column("command_id")]
    public Guid? CommandId { get; set; }

    [Column("message")]
    public string? Message { get; set; }

    [Column("payload_json", TypeName = "jsonb")]
    public string? PayloadJson { get; set; }

    [Column("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    
    
    [ForeignKey(nameof(RoomPk))]
    public RoomsEntity? Room { get; set; }
    
    [ForeignKey(nameof(DevicePk))]
    public DeviceEntity? Device { get; set; }
}
