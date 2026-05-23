namespace EzBias.Application.Features.Auth.Dtos;

public record RegisterRequest(string FullName, string Username, string Email, string Password);
public record LoginRequest(string EmailOrUsername, string Password);
public record RefreshRequest(string? RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
public record RequestEmailVerificationRequest(string Email);
public record VerifyEmailRequest(string Email, string Code);
public record SimpleMessageResponse(string Message);

public record AuthResult(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    long UserId,
    string Username,
    string Email,
    string Role);

public record AuthResponse(
    string AccessToken,
    int ExpiresInSeconds,
    long UserId,
    string Username,
    string Email,
    string Role);

public record MeResponse(long UserId, string Username, string Email, string FullName, string Role);
