using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class Reaction
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string? Name { get; set; }

    [InverseProperty("Reaction")]
    public virtual ICollection<PrivateMessage> PrivateMessages { get; set; } = new List<PrivateMessage>();
}
