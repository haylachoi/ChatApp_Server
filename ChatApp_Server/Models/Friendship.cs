using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class Friendship
{
    [Key]
    public int Id { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public bool? IsAccepted { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ReceiverId")]
    [InverseProperty("FriendshipReceivers")]
    public virtual User Receiver { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("FriendshipSenders")]
    public virtual User Sender { get; set; } = null!;
}
