using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos.Auth;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Api.Services;

public class AuthService(
    AppDbContext context,
    IConfiguration configuration,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailAlreadyExists = await context.Users.AnyAsync(user => user.Email == normalizedEmail);

        if (emailAlreadyExists)
        {
            return null;
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Carts.Add(new Cart { UserId = user.Id });
        await context.SaveChangesAsync();

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        var passwordVerification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordVerification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        return new AuthResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Token = GenerateToken(user, expiresAt),
            ExpiresAt = expiresAt
        };
    }

    private string GenerateToken(User user, DateTime expiresAt)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenExpirationHours()
    {
        var configuredValue = configuration["Jwt:ExpirationHours"];
        return int.TryParse(configuredValue, out var hours) ? hours : 8;
    }
}
