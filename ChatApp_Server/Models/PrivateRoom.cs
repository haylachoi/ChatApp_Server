using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("BiggerUserId", "SmallerUserId", Name = "idx_pr_twouser", IsUnique = true)]
public partial class PrivateRoom
{
    [Key]
    public int Id { get; set; }

    public int BiggerUserId { get; set; }

    public int SmallerUserId { get; set; }

    public long? LastMessageId { get; set; }

    public long? FirstUnseenBiggerUserMessageId { get; set; }

    public long? FirstUnseenSmallerUserMessageId { get; set; }

    [ForeignKey("BiggerUserId")]
    [InverseProperty("PrivateRoomBiggerUsers")]
    public virtual User BiggerUser { get; set; } = null!;

    [InverseProperty("PrivateRoom")]
    public virtual ICollection<PrivateMessage> PrivateMessages { get; set; } = new List<PrivateMessage>();

    [InverseProperty("PrivateRoom")]
    public virtual ICollection<PrivateRoomInfo> PrivateRoomInfos { get; set; } = new List<PrivateRoomInfo>();

    [ForeignKey("SmallerUserId")]
    [InverseProperty("PrivateRoomSmallerUsers")]
    public virtual User SmallerUser { get; set; } = null!;
}
