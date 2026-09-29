using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketApi.Configuration;
using TicketApi.Modules.Auth.Dto;
using TicketApi.Modules.Auth.Entities;
using TicketApi.Data;

namespace TicketApi.Modules.Auth;

public sealed class AdminAuthService
{
    private static readonly string DecoyPasswordHash = new PasswordHasher<AdminUser>()
        .HashPassword(new AdminUser(), Guid.NewGuid().ToString());

    private readonly TicketDbContext database;
    private readonly IPasswordHasher<AdminUser> hasher;
    private readonly AuthOptions options;

    public AdminAuthService(
        TicketDbContext database,
        IPasswordHasher<AdminUser> hasher,
        IOptions<AuthOptions> options
    )
    {
        this.database = database;
        this.hasher = hasher;
        this.options = options.Value;
    }

    public static SymmetricSecurityKey SigningKeyOf(AuthOptions options) =>
        new(Encoding.UTF8.GetBytes(options.SigningKey));

    public static string NormalizedEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Returns null when the email is unknown or the password is wrong; the
    /// caller must not distinguish the two.</summary>
    public async Task<AdminSessionDto?> SignInAsync(
        LoginRequest request,
        CancellationToken ct = default
    )
    {
        var email = NormalizedEmail(request.Email);
        var admin = await database.Admins.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            ct
        );

        // Hash on the unknown-email path too, so the response time does not say
        // whether the account exists -- ADR-0002 section 5, hashing and secrets.
        if (admin is null)
        {
            hasher.VerifyHashedPassword(new AdminUser(), DecoyPasswordHash, request.Password);
            return null;
        }

        var verification = hasher.VerifyHashedPassword(
            admin,
            admin.PasswordHash,
            request.Password
        );
        if (verification == PasswordVerificationResult.Failed)
            return null;

        return new AdminSessionDto(TokenFor(admin), admin.Email);
    }

    private string TokenFor(AdminUser admin)
    {
        var token = new JwtSecurityToken(
            issuer: AuthOptions.Issuer,
            audience: AuthOptions.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, admin.Id),
                new Claim(JwtRegisteredClaimNames.Email, admin.Email),
            ],
            expires: DateTime.UtcNow.Add(AuthOptions.TokenLifetime),
            signingCredentials: new SigningCredentials(
                SigningKeyOf(options),
                SecurityAlgorithms.HmacSha256
            )
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
