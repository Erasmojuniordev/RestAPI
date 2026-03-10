using FluentValidation;
using RestauranteAPI.DTOs.Comanda;
using RestauranteAPI.Enums;

namespace RestauranteAPI.Validators;

public class AbrirComandaValidator : AbstractValidator<AbrirComandaDto>
{
    public AbrirComandaValidator()
    {
        RuleFor(x => x.NumeroDaMesa)
            .GreaterThan(0).WithMessage("Número da mesa deve ser maior que 0.")
            .LessThanOrEqualTo(100).WithMessage("Número da mesa não pode ser maior que 100.");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("Observação não pode ter mais de 500 caracteres.")
            .When(x => x.Observacao is not null);
    }
}

public class AdicionarItemComandaValidator : AbstractValidator<AdicionarItemComandaDto>
{
    public AdicionarItemComandaValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item é obrigatório.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que 0.")
            .LessThanOrEqualTo(50).WithMessage("Quantidade não pode ser maior que 50.");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("Observação não pode ter mais de 500 caracteres.")
            .When(x => x.Observacao is not null);
    }
}

public class AtualizarStatusValidator : AbstractValidator<AtualizarStatusDto>
{
    public AtualizarStatusValidator()
    {
        RuleFor(x => x.NovoStatus)
            .IsInEnum().WithMessage("Status inválido.");
    }
}