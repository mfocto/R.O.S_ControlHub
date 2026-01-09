using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ROS_ControlHub.Application.Entities;

public class ControlStateEventEntity
{
    [Key]
    [Column("event_pk")]
    public long EventPk { get; set; }
    [Column("device_pk")]
    public long DevicePk { get; set; }
    [Column("command_id")]
    public string CommandId { get; set; }
    [Column("prev_version")]
    public int PrevVersion { get; set; }
    [Column("next_version")]
    public int NextVersion { get; set; }
    [Column("patch_json", TypeName = "jsonb")]
    public string PatchJson { get; set; }
    [Column("state_json", TypeName = "jsonb")]
    public string StateJson { get; set; }
    [Column("actor_type")]
    public string ActorType { get; set; }
    [Column("actor_id")]
    public string ActorId { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    // 관계 설정
    public DeviceEntity? Device { get; set; }
}