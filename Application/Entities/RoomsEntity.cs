using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ROS_ControlHub.Application.Entities;

[Table("rooms")]
public class RoomsEntity
{   
    [Key]
    [Column("room_pk")]
    public long RoomPk { get; set; }
    
    [Column("room_id")]
    public string RoomId { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation Property
    // 1:N 관계에서 '1' 쪽은 'N'의 컬렉션을 가질 수 있습니다.
    public ICollection<DeviceEntity> Devices { get; set; } = new List<DeviceEntity>();
    public ICollection<SystemLogEntity> SystemLogs { get; set; } = new List<SystemLogEntity>();
}