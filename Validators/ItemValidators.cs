using FluentValidation;
using RestauranteAPI.DTOs.Item;

namespace RestauranteAPI.Validators;

public class CriarItemValidator : AbstractValidator<CriarItemDto>
{
    public CriarItemValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres.");

        RuleFor(x => x.Preco)
            .GreaterThan(0).WithMessage("Preço deve ser maior que 0.")
            .LessThanOrEqualTo(9999.99m).WithMessage("Preço não pode ser maior que R$ 9.999,99.");

        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("Descrição não pode ter mais de 500 caracteres.")
            .When(x => x.Descricao is not null);

        RuleFor(x => x.Categoria)
            .MaximumLength(50).WithMessage("Categoria não pode ter mais de 50 caracteres.")
            .When(x => x.Categoria is not null);

        RuleFor(x => x.ImagemUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("URL da imagem inválida.")
            .When(x => x.ImagemUrl is not null);
    }
}

public class AtualizarItemValidator : AbstractValidator<AtualizarItemDto>
{
    public AtualizarItemValidator()
    {
        RuleFor(x => x.Nome)
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres.")
            .When(x => x.Nome is not null);

        RuleFor(x => x.Preco)
            .GreaterThan(0).WithMessage("Preço deve ser maior que 0.")
            .LessThanOrEqualTo(9999.99m).WithMessage("Preço não pode ser maior que R$ 9.999,99.")
            .When(x => x.Preco.HasValue);

        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("Descrição não pode ter mais de 500 caracteres.")
            .When(x => x.Descricao is not null);

        RuleFor(x => x.Categoria)
            .MaximumLength(50).WithMessage("Categoria não pode ter mais de 50 caracteres.")
            .When(x => x.Categoria is not null);

        RuleFor(x => x.ImagemUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("URL da imagem inválida.")
            .When(x => x.ImagemUrl is not null);
    }
}