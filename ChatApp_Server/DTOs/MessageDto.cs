using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChatApp_Server.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.DTOs;

public partial class MessageDto
{ 
    public long Id { get; set; }

    public string Content { get; set; } = null!;

    public bool IsImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int SenderId { get; set; }

    public bool? IsBlocked { get; set; }
    public int RoomId { get; set; }


    public virtual IEnumerable<MessageDetailDto> MessageDetails { get; set; } = Enumerable.Empty<MessageDetailDto>();

}
