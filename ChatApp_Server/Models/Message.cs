﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("Id", "RoomId", Name = "unq_messages_id_roomid", IsUnique = true)]
public partial class Message
{
    [Key]
    public long Id { get; set; }

    [Column(TypeName = "character varying")]
    public string Content { get; set; } = null!;

    public bool IsImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int SenderId { get; set; }

    public int RoomId { get; set; }

    public long? QuoteId { get; set; }

    [InverseProperty("Quote")]
    public virtual ICollection<Message> InverseQuote { get; set; } = new List<Message>();

    [InverseProperty("Message")]
    public virtual ICollection<MessageDetail> MessageDetails { get; set; } = new List<MessageDetail>();

    [ForeignKey("QuoteId")]
    [InverseProperty("InverseQuote")]
    public virtual Message? Quote { get; set; }

    [ForeignKey("RoomId")]
    [InverseProperty("Messages")]
    public virtual Room Room { get; set; } = null!;

    [InverseProperty("FirstMessage")]
    public virtual ICollection<Room> RoomFirstMessages { get; set; } = new List<Room>();

    [InverseProperty("LastMessage")]
    public virtual ICollection<Room> RoomLastMessages { get; set; } = new List<Room>();

    [InverseProperty("FirstUnseenMessage")]
    public virtual ICollection<RoomMemberInfo> RoomMemberInfos { get; set; } = new List<RoomMemberInfo>();

    [ForeignKey("SenderId")]
    [InverseProperty("Messages")]
    public virtual User Sender { get; set; } = null!;
}
