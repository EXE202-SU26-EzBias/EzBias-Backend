namespace EzBias.Infrastructure.Persistence.SeedData;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; }
    public AdminSeedOptions Admin { get; set; } = new();

    public void Validate()
    {
        if (Admin is null)
            throw new InvalidOperationException("SeedData:Admin configuration is required.");

        if (string.IsNullOrWhiteSpace(Admin.FullName))
            throw new InvalidOperationException("SeedData:Admin:FullName is required.");

        if (string.IsNullOrWhiteSpace(Admin.Username))
            throw new InvalidOperationException("SeedData:Admin:Username is required.");

        if (string.IsNullOrWhiteSpace(Admin.Email))
            throw new InvalidOperationException("SeedData:Admin:Email is required.");

        if (string.IsNullOrWhiteSpace(Admin.Password))
            throw new InvalidOperationException("SeedData:Admin:Password is required.");

        if (Admin.Password.Length < 6)
            throw new InvalidOperationException("SeedData:Admin:Password must be at least 6 characters.");
    }
}

public sealed class AdminSeedOptions
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
