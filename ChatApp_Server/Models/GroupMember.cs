using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("GroupId", "MemberId", Name = "idx_gm_groupid_memberid", IsUnique = true)]
public partial class GroupMember
{
    public int? GroupId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [Key]
    public int Id { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupMembers")]
    public virtual Group? Group { get; set; }

    [ForeignKey("MemberId")]
    [InverseProperty("GroupMembers")]
    public virtual User? Member { get; set; }
}
