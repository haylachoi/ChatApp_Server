using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class PrivateMessage
{
    [Key]
    public long Id { get; set; }

    [Column(TypeName = "character varying")]
    public string Content { get; set; } = null!;

    public bool IsImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public bool IsReaded { get; set; }

    public bool? IsBlocked { get; set; }

    public int PrivateRoomId { get; set; }

    public int? ReactionId { get; set; }

    [ForeignKey("PrivateRoomId")]
    [InverseProperty("PrivateMessages")]
    public virtual PrivateRoom PrivateRoom { get; set; } = null!;

    [InverseProperty("FirstMessage")]
    public virtual ICollection<PrivateRoom> PrivateRoomFirstMessages { get; set; } = new List<PrivateRoom>();

    [InverseProperty("FirstUnseenMessage")]
    public virtual ICollection<PrivateRoomInfo> PrivateRoomInfoFirstUnseenMessages { get; set; } = new List<PrivateRoomInfo>();

    [InverseProperty("LastUnseenMessage")]
    public virtual ICollection<PrivateRoomInfo> PrivateRoomInfoLastUnseenMessages { get; set; } = new List<PrivateRoomInfo>();

    [InverseProperty("LastMessage")]
    public virtual ICollection<PrivateRoom> PrivateRoomLastMessages { get; set; } = new List<PrivateRoom>();

    [ForeignKey("ReactionId")]
    [InverseProperty("PrivateMessages")]
    public virtual Reaction? Reaction { get; set; }

    [ForeignKey("ReceiverId")]
    [InverseProperty("PrivateMessageReceivers")]
    public virtual User Receiver { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("PrivateMessageSenders")]
    public virtual User Sender { get; set; } = null!;
}
