using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Table("Blocklist")]
public partial class Blocklist
{
    [Key]
    public int Id { get; set; }

    public int BlockerId { get; set; }

    public int BlockedId { get; set; }

    public DateTime? CreateAt { get; set; }

    [ForeignKey("BlockedId")]
    [InverseProperty("BlocklistBlockeds")]
    public virtual User Blocked { get; set; } = null!;

    [ForeignKey("BlockerId")]
    [InverseProperty("BlocklistBlockers")]
    public virtual User Blocker { get; set; } = null!;
}
