using EzBias.Application.Features.Admin.Dtos;
using EzBias.Application.Features.Auth.Services;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Admin;

public class AdminApplicationService : IAdminApplicationService
{
    private readonly IAdminRepository _adminRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;

    public AdminApplicationService(IAdminRepository adminRepository, IPasswordHasher passwordHasher, IUnitOfWork uow)
    {
        _adminRepository = adminRepository;
        _passwordHasher = passwordHasher;
        _uow = uow;
    }

    public async Task<AdminDashboardOverviewResponse> GetDashboardOverviewAsync(CancellationToken ct)
    {
        var x = await _adminRepository.GetDashboardOverviewAsync(ct);
        var topSellers = x.TopSellersByNetRevenue
            .Select(s => new AdminTopSellerCommissionResponse(s.SellerId, s.Username, s.FullName, s.OrderCount, s.GrossRevenue, s.CommissionRevenue, s.NetRevenue))
            .ToList();
        var monthlySales = x.MonthlySales
            .Select(m => new AdminMonthlySalesResponse(m.Month, m.Label, m.OrderCount, m.GrossSales, m.CommissionRevenue, m.SellerNetAmount))
            .ToList();

        return new AdminDashboardOverviewResponse(
            x.TotalUsers,
            x.NewUsersToday,
            x.NewUsersLast7Days,
            x.NewUsersLast30Days,
            x.TotalOrders,
            x.PendingOrders,
            x.PaidOrders,
            x.ProcessingOrders,
            x.ShippedOrders,
            x.DeliveredOrders,
            x.ReturnRequestedOrders,
            x.CompletedOrders,
            x.CanceledOrders,
            x.RefundedOrders,
            x.GrossRevenue,
            x.RefundedAmount,
            x.NetRevenue,
            x.TotalCommissionRevenue,
            x.CommissionRevenueToday,
            x.CommissionRevenueLast7Days,
            x.CommissionRevenueLast30Days,
            x.OpenDisputes,
            x.PendingRefunds,
            x.PendingPayouts,
            topSellers,
            monthlySales);
    }

    public async Task<AdminUserListResponse> GetUsersAsync(AdminUserListQuery query, CancellationToken ct)
    {
        var safePage = query.Page <= 0 ? 1 : query.Page;
        var safePageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

        UserRole? role = null;
        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<UserRole>(query.Role, true, out var parsedRole))
            role = parsedRole;

        var (items, totalItems) = await _adminRepository.GetUsersAsync(query.Keyword, role, query.IsDeleted, safePage, safePageSize, ct);
        var totalPages = (int)Math.Ceiling(totalItems / (double)safePageSize);

        var mapped = items.Select(x => new AdminUserListItemResponse(x.Id, x.FullName, x.Username, x.Email, x.Role.ToString(), x.DeletedAt != null, x.CreatedAt)).ToList();
        return new AdminUserListResponse(mapped, safePage, safePageSize, totalItems, totalPages);
    }

    public async Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> GetUserDetailAsync(long userId, CancellationToken ct)
    {
        var user = await _adminRepository.GetUserDetailAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);
        var data = new AdminUserDetailResponse(user.Id, user.FullName, user.Username, user.Email, user.Role.ToString(), user.Phone, user.City, user.DeletedAt != null, user.CreatedAt, user.UpdatedAt, user.OrdersAsBuyer.Count, user.OrdersAsSeller.Count, user.DisputesOpened.Count, user.AvgSellerRating, user.TotalRatings);
        return (true, null, data);
    }

    public async Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "FullName, Username, Email, Password are required.", null);

        if (request.Password.Length < 6)
            return (false, "Password must be at least 6 chars.", null);

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return (false, "Invalid role.", null);

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();
        var exists = await _adminRepository.ExistsByEmailOrUsernameAsync(email, username, null, ct);
        if (exists) return (false, "Email or username already exists.", null);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Username = request.Username.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            Phone = request.Phone?.Trim() ?? string.Empty,
            City = request.City?.Trim() ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _adminRepository.AddUser(user);
        await _uow.SaveChangesAsync(ct);

        return await GetUserDetailAsync(user.Id, ct);
    }

    public async Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> UpdateUserAsync(long userId, AdminUpdateUserRequest request, CancellationToken ct)
    {
        var user = await _adminRepository.GetUserByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);

        if (!string.IsNullOrWhiteSpace(request.Role) && !Enum.TryParse<UserRole>(request.Role, true, out _))
            return (false, "Invalid role.", null);

        var newEmail = string.IsNullOrWhiteSpace(request.Email) ? user.Email.ToLowerInvariant() : request.Email.Trim().ToLowerInvariant();
        var newUsername = string.IsNullOrWhiteSpace(request.Username) ? user.Username.ToLowerInvariant() : request.Username.Trim().ToLowerInvariant();
        var exists = await _adminRepository.ExistsByEmailOrUsernameAsync(newEmail, newUsername, user.Id, ct);
        if (exists) return (false, "Email or username already exists.", null);

        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(request.Username)) user.Username = request.Username.Trim();
        if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = newEmail;
        if (!string.IsNullOrWhiteSpace(request.Phone)) user.Phone = request.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(request.City)) user.City = request.City.Trim();
        if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var role)) user.Role = role;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);
        var detail = await GetUserDetailAsync(user.Id, ct);
        return detail;
    }

    public async Task<(bool Success, string? Error)> SoftDeleteUserAsync(long userId, long adminId, CancellationToken ct)
    {
        if (userId == adminId) return (false, "You cannot delete your own account.");

        var user = await _adminRepository.GetUserByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.");
        if (user.DeletedAt != null) return (false, "User already deleted.");

        user.DeletedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, AdminUserDetailResponse? Data)> RestoreUserAsync(long userId, CancellationToken ct)
    {
        var user = await _adminRepository.GetUserByIdAsync(userId, ct);
        if (user is null) return (false, "User not found.", null);
        if (user.DeletedAt is null) return (false, "User is not deleted.", null);

        user.DeletedAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return await GetUserDetailAsync(userId, ct);
    }
}
