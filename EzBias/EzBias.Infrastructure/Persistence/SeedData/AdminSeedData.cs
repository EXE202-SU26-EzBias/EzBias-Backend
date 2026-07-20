using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Persistence.SeedData;

public static class AdminSeedData
{
    public static async Task SeedAsync(EzBiasDbContext db, AdminSeedOptions options, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var fullName = options.FullName.Trim();
        var username = options.Username.Trim().ToLowerInvariant();
        var email = options.Email.Trim().ToLowerInvariant();

        var admin = await db.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, ct);

        var usernameOwner = await db.Users
            .FirstOrDefaultAsync(x => x.Username.ToLower() == username, ct);

        if (usernameOwner is not null && usernameOwner.Id != admin?.Id)
            throw new InvalidOperationException("SeedData:Admin:Username is already used by another account.");

        if (admin is null)
        {
            db.Users.Add(new User
            {
                FullName = fullName,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(options.Password),
                Role = UserRole.Admin,
                EmailVerifiedAt = now,
                CreatedAt = now
            });

            await db.SaveChangesAsync(ct);
            return;
        }

        var changed = false;

        if (admin.FullName != fullName) { admin.FullName = fullName; changed = true; }
        if (admin.Username != username) { admin.Username = username; changed = true; }
        if (admin.Email != email) { admin.Email = email; changed = true; }
        if (admin.Role != UserRole.Admin) { admin.Role = UserRole.Admin; changed = true; }
        if (admin.EmailVerifiedAt is null) { admin.EmailVerifiedAt = now; changed = true; }
        if (admin.DeletedAt is not null) { admin.DeletedAt = null; changed = true; }

        if (!PasswordMatches(options.Password, admin.PasswordHash))
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(options.Password);
            changed = true;
        }

        if (!changed)
            return;

        admin.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
    }

    private static bool PasswordMatches(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
