using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.DTOs;

public partial class PrivateMessageDto
{ 
    public long? Id { get; set; }

   
    public string Content { get; set; } = null!;

    public bool IsImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public bool IsReaded { get; set; }

    public bool? IsBlocked { get; set; }
    public int PrivateRoomId { get; set; }

    public int? EmotionId { get; set; }

}
