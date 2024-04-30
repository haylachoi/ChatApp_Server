using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Models;

[Table("RefreshToken")]
public partial class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Column(TypeName = "character varying")]
    public string Token { get; set; } = null!;

    [Column(TypeName = "character varying")]
    public string JwtId { get; set; } = null!;

    public int UserId { get; set; }

    public bool IsUsed { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User User { get; set; } = null!;
}
