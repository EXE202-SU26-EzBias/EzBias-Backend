using EzBias.Application.Features.Users.Dtos;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Users;

public class UserProfileApplicationService : IUserProfileApplicationService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public UserProfileApplicationService(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<(bool Success, string? Error, UserProfileResponse? Data)> GetMeAsync(long userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);
        return (true, null, Map(user));
    }

    public async Task<(bool Success, string? Error, UserProfileResponse? Data)> UpdateMeAsync(long userId, UpdateUserProfileRequest request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);

        user.FullName = request.FullName?.Trim() ?? string.Empty;
        user.Phone = request.Phone?.Trim() ?? string.Empty;
        user.Address = request.Address?.Trim() ?? string.Empty;
        user.City = request.City?.Trim() ?? string.Empty;
        user.Zip = request.Zip?.Trim() ?? string.Empty;
        user.BankName = request.BankName?.Trim() ?? string.Empty;
        user.BankAccountNumber = request.BankAccountNumber?.Trim() ?? string.Empty;
        user.BankAccountName = request.BankAccountName?.Trim() ?? string.Empty;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return (true, null, Map(user));
    }

    private static UserProfileResponse Map(Domain.Entities.User user)
        => new(user.Id, user.FullName, user.Username, user.Email, user.Phone, user.Address, user.City, user.Zip, user.AvatarUrl, user.AvatarBg, user.BankName, user.BankAccountNumber, user.BankAccountName);
}
