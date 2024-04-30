﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

public partial class Emotion
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string? Name { get; set; }

    [InverseProperty("Emotion")]
    public virtual ICollection<PrivateMessage> PrivateMessages { get; set; } = new List<PrivateMessage>();
}
