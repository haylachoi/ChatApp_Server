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

    [InverseProperty("Member")]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    [InverseProperty("Sender")]
    public virtual ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();

    [InverseProperty("GroupOwner")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [InverseProperty("Receiver")]
    public virtual ICollection<PrivateMessage> PrivateMessageReceivers { get; set; } = new List<PrivateMessage>();

    [InverseProperty("Sender")]
    public virtual ICollection<PrivateMessage> PrivateMessageSenders { get; set; } = new List<PrivateMessage>();

    [InverseProperty("BiggerUser")]
    public virtual ICollection<PrivateRoom> PrivateRoomBiggerUsers { get; set; } = new List<PrivateRoom>();

    [InverseProperty("User")]
    public virtual ICollection<PrivateRoomInfo> PrivateRoomInfos { get; set; } = new List<PrivateRoomInfo>();

    [InverseProperty("SmallerUser")]
    public virtual ICollection<PrivateRoom> PrivateRoomSmallerUsers { get; set; } = new List<PrivateRoom>();

    [InverseProperty("Member")]
    public virtual ICollection<ReadedMessage> ReadedMessages { get; set; } = new List<ReadedMessage>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
