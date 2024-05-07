using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("Email", Name = "users_email_key", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(30)]
    public string? Fullname { get; set; }

    [StringLength(128)]
    public string Password { get; set; } = null!;

    [StringLength(20)]
    public string Salt { get; set; } = null!;

    [StringLength(40)]
    public string Email { get; set; } = null!;

    [Column(TypeName = "character varying")]
    public string? Avatar { get; set; }

    public bool IsOnline { get; set; }

    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Blocked")]
    public virtual ICollection<Blocklist> BlocklistBlockeds { get; set; } = new List<Blocklist>();

    [InverseProperty("Blocker")]
    public virtual ICollection<Blocklist> BlocklistBlockers { get; set; } = new List<Blocklist>();

    [InverseProperty("Receiver")]
    public virtual ICollection<Friendship> FriendshipReceivers { get; set; } = new List<Friendship>();

    [InverseProperty("Sender")]
    public virtual ICollection<Friendship> FriendshipSenders { get; set; } = new List<Friendship>();

    [InverseProperty("GroupOwner")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [InverseProperty("User")]
    public virtual ICollection<MessageDetail> MessageDetails { get; set; } = new List<MessageDetail>();

    [InverseProperty("Sender")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<RoomMemberInfo> RoomMemberInfos { get; set; } = new List<RoomMemberInfo>();
}
