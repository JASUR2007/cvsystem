using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IList<string> Roles);

public sealed record AuthResponse(string Token, CurrentUserResponse User);

public sealed record ExchangeRequest([Required] string Code);
