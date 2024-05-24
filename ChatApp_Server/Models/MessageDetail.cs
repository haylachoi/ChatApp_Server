using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("MessageId", "UserId", Name = "idx_unique_message_user", IsUnique = true)]
public partial class MessageDetail
{
    [Key]
    public long Id { get; set; }

    public long MessageId { get; set; }

    public int UserId { get; set; }

    public int? ReactionId { get; set; }

    public int RoomId { get; set; }

    [ForeignKey("MessageId, RoomId")]
    [InverseProperty("MessageDetails")]
    public virtual Message Message { get; set; } = null!;

    [ForeignKey("ReactionId")]
    [InverseProperty("MessageDetails")]
    public virtual Reaction? Reaction { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("MessageDetails")]
    public virtual User User { get; set; } = null!;
}
