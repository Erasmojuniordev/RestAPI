using FluentValidation;
using RestauranteAPI.DTOs.Auth;

namespace RestauranteAPI.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.");
    }
}

public class CriarUsuarioValidator : AbstractValidator<CriarUsuarioDto>
{
    private static readonly string[] _rolesValidas = ["Admin", "Garcom", "Cozinha", "Caixa"];

    public CriarUsuarioValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(256).WithMessage("Email não pode ter mais de 256 caracteres.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.")
            .Matches(@"\d").WithMessage("Senha deve conter pelo menos um número.");

        RuleFor(x => x.NomeCompleto)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(150).WithMessage("Nome não pode ter mais de 150 caracteres.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role é obrigatória.")
            .Must(role => _rolesValidas.Contains(role))
            .WithMessage($"Role inválida. Valores aceitos: {string.Join(", ", _rolesValidas)}");
    }
}