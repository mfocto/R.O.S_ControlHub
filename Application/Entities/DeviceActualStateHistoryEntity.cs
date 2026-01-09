using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ROS_ControlHub.Application.Entities;

public class DeviceActualStateHistoryEntity
{
    [Key]
    [Column("actual_pk")]
    public long ActualPk { get; set; }
    
    [Column("device_pk")]
    public long DevicePk { get; set; }
    
    [Column("state_json", TypeName = "jsonb")]
    public string StateJson { get; set; }
    
    [Column("reported_at")]
    public DateTimeOffset ReportedAt { get; set; }
    
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    // 관계 설정
    public DeviceEntity? Device { get; set; }
}