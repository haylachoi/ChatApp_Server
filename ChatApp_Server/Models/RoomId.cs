using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Keyless]
[Table("room_id")]
public partial class RoomId
{
    [Column("RoomId")]
    public int? RoomId1 { get; set; }
}
