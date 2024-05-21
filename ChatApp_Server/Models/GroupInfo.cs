using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class GroupInfo
{
    [Key]
    public int GroupId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public string? Avatar { get; set; }

    public int GroupOnwerId { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupInfo")]
    public virtual Room Group { get; set; } = null!;

    [ForeignKey("GroupOnwerId")]
    [InverseProperty("GroupInfos")]
    public virtual User GroupOnwer { get; set; } = null!;
}
