﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Index("UserId", "PrivateRoomId", Name = "unq_user_room", IsUnique = true)]
public partial class PrivateRoomInfo
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PrivateRoomId { get; set; }

    public long? FirstUnseenMessageId { get; set; }

    public long UnseenMessageCount { get; set; }

    public long? LastUnseenMessageId { get; set; }

    [ForeignKey("PrivateRoomId")]
    [InverseProperty("PrivateRoomInfos")]
    public virtual PrivateRoom PrivateRoom { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("PrivateRoomInfos")]
    public virtual User User { get; set; } = null!;
}