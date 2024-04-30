using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class GroupMessage
{
    [Key]
    public long Id { get; set; }

    [Column(TypeName = "character varying")]
    public string Content { get; set; } = null!;

    public bool IsImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int SenderId { get; set; }

    public int GroupId { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupMessages")]
    public virtual Group Group { get; set; } = null!;

    [InverseProperty("GroupMessage")]
    public virtual ICollection<ReadedMessage> ReadedMessages { get; set; } = new List<ReadedMessage>();

    [ForeignKey("SenderId")]
    [InverseProperty("GroupMessages")]
    public virtual User Sender { get; set; } = null!;
}
