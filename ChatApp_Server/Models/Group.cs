using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class Group
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public int GroupOwnerId { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    [InverseProperty("Group")]
    public virtual ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();

    [ForeignKey("GroupOwnerId")]
    [InverseProperty("Groups")]
    public virtual User GroupOwner { get; set; } = null!;
}
