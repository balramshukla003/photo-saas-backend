// ─────────────────────────────────────────────────────────────────────────────
//  SAFE TO EDIT — this partial class is never touched by scaffold.
//  Add custom queries, raw SQL helpers, or seed logic here.
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Data;

public partial class PhotoPrintDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Add any custom Fluent API config here that should survive re-scaffolding.
        // Example: table-level comments, custom value converters, query filters.
    }

    // ── Convenience query: get user with their active license ──────────────
    public IQueryable<User> UsersWithLicenses()
        => Users.Include(u => u.Licenses);

    // ── Convenience query: find active license for a user ─────────────────
    public async Task<License?> GetActiveLicenseAsync(string userId, CancellationToken ct = default)
        => await Licenses
            .Where(l => l.UserId.ToString() == userId && l.IsActive == true)
            .OrderByDescending(l => l.ExpiresAt)
            .FirstOrDefaultAsync(ct);
}
