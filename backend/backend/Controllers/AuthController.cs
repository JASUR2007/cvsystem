using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    TokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        if (firstName.Length == 0 || lastName.Length == 0)
        {
            return BadRequest(new { message = "First name and last name are required." });
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        IdentityResult created;
        try
        {
            created = await userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        if (!created.Succeeded)
        {
            return BadRequest(new { errors = created.Errors.Select(error => error.Description) });
        }

        var assigned = await userManager.AddToRoleAsync(user, Roles.Candidate);
        if (!assigned.Succeeded)
        {
            return StatusCode(500, new { message = "Account role could not be assigned." });
        }

        await transaction.CommitAsync(cancellationToken);
        return Ok(await CreateResponseAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.IsBlocked || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(await CreateResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null)
        {
            return Unauthorized();
        }

        await db.Users.Where(user => user.Id == id.Value)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.AuthVersion, user => user.AuthVersion + 1), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var id = CurrentUserId();
        if (id is null)
        {
            return Unauthorized();
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await CreateCurrentUserAsync(user));
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private async Task<AuthResponse> CreateResponseAsync(AppUser user)
    {
        var currentUser = await CreateCurrentUserAsync(user);
        return new AuthResponse(await tokenService.CreateAsync(user), currentUser);
    }

    private async Task<CurrentUserResponse> CreateCurrentUserAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserResponse(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles);
    }
}

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
