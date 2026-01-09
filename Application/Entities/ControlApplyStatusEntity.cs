using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ROS_ControlHub.Application.Entities;

public class ControlApplyStatusEntity
{
    [Key]
    [Column("apply_pk")]
    public int ApplyPk { get; set; }
    
    [Column("device_pk")]
    public long DevicePk { get; set; }
    
    [Column("target_system")]
    public string TargetSystem { get; set; }
    
    [Column("status")]
    public string Status { get; set; }
    
    [Column("target_version")]
    public int TargetVersion { get; set; }
    
    [Column("last_applied_version")]
    public int LastAppliedVersion { get; set; }
    
    [Column("retry_count")]
    public int RetryCount { get; set; }
    
    [Column("last_error")]
    public string LastError { get; set; }
    
    [Column("last_attempt_at")]
    public DateTimeOffset LastAttemptAt { get; set; }
    
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
    
    
    // 관계설정
    public DeviceEntity? Device { get; set; }
}