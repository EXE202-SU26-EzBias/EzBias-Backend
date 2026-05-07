namespace EzBias.Application.Features.Users.Dtos;

public record UserProfileResponse(long Id, string FullName, string Username, string Email, string Phone, string Address, string City, string Zip, string AvatarUrl, string AvatarBg, string BankName, string BankAccountNumber, string BankAccountName);
public record UpdateUserProfileRequest(string FullName, string Phone, string Address, string City, string Zip, string BankName, string BankAccountNumber, string BankAccountName);
