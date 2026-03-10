namespace RestauranteAPI.DTOs.Auth;

public record LoginDto(
    string Email,
    string Senha
);

public record CriarUsuarioDto(
    string Email,
    string Senha,
    string NomeCompleto,
    string Role
);

public record TokenResponseDto(
    string Token,
    DateTime Expiracao,
    string NomeCompleto,
    string Email,
    IEnumerable<string> Roles
);
