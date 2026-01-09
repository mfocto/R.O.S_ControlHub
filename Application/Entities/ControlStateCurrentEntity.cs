using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ROS_ControlHub.Application.Entities;

[Table("control_state_current")]
public class ControlStateCurrentEntity
{
    [Key]
    [Column("device_pk")]
    public long DevicePk { get; set; }

    [Column("version")]
    public long Version { get; set; }

    [Column("state_json", TypeName = "jsonb")]
    public string StateJson { get; set; } = "{}";

    [Column("last_command_id")]
    public Guid? LastCommandId { get; set; }

    [Column("last_actor_type")]
    public string LastActorType { get; set; } = "system";

    [Column("last_actor_id")]
    public string? LastActorId { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    
    // 관계설정
    public DeviceEntity? Device { get; set; }
}
