using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PhotoPrint.API.Models;

[Table("licenses")]
[Index("ExpiresAt", Name = "idx_expires_at")]
[Index("LicenseKey", Name = "idx_license_key", IsUnique = true)]
[Index("UserId", Name = "idx_user_id")]
public partial class License
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("license_key")]
    [StringLength(64)]
    public string LicenseKey { get; set; } = null!;

    [Required]
    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("issued_at", TypeName = "datetime")]
    public DateTime IssuedAt { get; set; }

    [Column("expires_at", TypeName = "datetime")]
    public DateTime ExpiresAt { get; set; }

    [Column("plan")]
    [StringLength(50)]
    public string Plan { get; set; } = null!;

    [Column("notes", TypeName = "text")]
    public string? Notes { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Licenses")]
    public virtual User User { get; set; } = null!;
}
