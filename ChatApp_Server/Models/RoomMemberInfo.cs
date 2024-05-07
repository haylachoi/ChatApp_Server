using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("UserId", "RoomId", Name = "unq_user_room", IsUnique = true)]
public partial class RoomMemberInfo
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public long? FirstUnseenMessageId { get; set; }

    public long UnseenMessageCount { get; set; }

    public long? LastUnseenMessageId { get; set; }

    [Column("canDisplayRoom")]
    public bool CanDisplayRoom { get; set; }

    [Column("canShowNofitication")]
    public bool CanShowNofitication { get; set; }

    [ForeignKey("FirstUnseenMessageId")]
    [InverseProperty("RoomMemberInfoFirstUnseenMessages")]
    public virtual Message? FirstUnseenMessage { get; set; }

    [ForeignKey("LastUnseenMessageId")]
    [InverseProperty("RoomMemberInfoLastUnseenMessages")]
    public virtual Message? LastUnseenMessage { get; set; }

    [ForeignKey("RoomId")]
    [InverseProperty("RoomMemberInfos")]
    public virtual Room Room { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("RoomMemberInfos")]
    public virtual User User { get; set; } = null!;
}
