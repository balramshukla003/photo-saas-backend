using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PhotoPrint.API.Models;

[Table("users")]
[Index("Email", Name = "email", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(512)]
    public string PasswordHash { get; set; } = null!;

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [Required]
    [Column("is_active")]
    public bool? IsActive { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<License> Licenses { get; set; } = new List<License>();
}
