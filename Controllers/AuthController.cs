using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RestauranteAPI.DTOs.Auth;
using RestauranteAPI.Models;

namespace RestauranteAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null || !user.Ativo || !await _userManager.CheckPasswordAsync(user, dto.Senha))
            return Unauthorized(new { erro = "Credenciais inválidas." });

        var roles = await _userManager.GetRolesAsync(user);
        var token = GerarToken(user, roles);

        return Ok(new TokenResponseDto(
            token.Token,
            token.Expiracao,
            user.NomeCompleto,
            user.Email!,
            roles
        ));
    }

    [HttpPost("usuarios")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto dto)
    {
        var rolesValidas = new[] { "Admin", "Garcom", "Cozinha", "Caixa" };
        if (!rolesValidas.Contains(dto.Role))
            return BadRequest(new { erro = $"Role inválida. Valores aceitos: {string.Join(", ", rolesValidas)}" });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            NomeCompleto = dto.NomeCompleto
        };

        var result = await _userManager.CreateAsync(user, dto.Senha);
        if (!result.Succeeded)
            return BadRequest(new { erros = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Created("", new { id = user.Id, email = user.Email, role = dto.Role });
    }

    // ── JWT ──────────────────────────────────────────────────

    private (string Token, DateTime Expiracao) GerarToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiracao = DateTime.UtcNow.AddHours(
            double.Parse(_config["Jwt:ExpiresInHours"] ?? "8"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.NomeCompleto),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Adiciona os roles como claims
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiracao,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracao);
    }
}
