using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class ReadedMessage
{
    public long GroupMessageId { get; set; }

    public int MemberId { get; set; }

    public bool? IsReaded { get; set; }

    [Key]
    public long Id { get; set; }

    [ForeignKey("GroupMessageId")]
    [InverseProperty("ReadedMessages")]
    public virtual GroupMessage GroupMessage { get; set; } = null!;

    [ForeignKey("MemberId")]
    [InverseProperty("ReadedMessages")]
    public virtual User Member { get; set; } = null!;
}
