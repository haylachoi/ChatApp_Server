using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class Room
{
    [Key]
    public int Id { get; set; }

    public long? LastMessageId { get; set; }

    public long? FirstMessageId { get; set; }

    public bool IsGroup { get; set; }

    public string? Avatar { get; set; }

    public string? Name { get; set; }

    [ForeignKey("FirstMessageId")]
    [InverseProperty("RoomFirstMessages")]
    public virtual Message? FirstMessage { get; set; }

    [ForeignKey("LastMessageId")]
    [InverseProperty("RoomLastMessages")]
    public virtual Message? LastMessage { get; set; }

    [InverseProperty("Room")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [InverseProperty("Room")]
    public virtual ICollection<RoomMemberInfo> RoomMemberInfos { get; set; } = new List<RoomMemberInfo>();
}
