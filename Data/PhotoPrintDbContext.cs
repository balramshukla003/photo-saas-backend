using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Data;

public partial class PhotoPrintDbContext : DbContext
{
    public PhotoPrintDbContext(DbContextOptions<PhotoPrintDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<License> Licenses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.IssuedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Plan).HasDefaultValueSql("'standard'");

            entity.HasOne(d => d.User).WithMany(p => p.Licenses).HasConstraintName("licenses_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
