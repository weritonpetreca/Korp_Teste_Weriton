using Faturamento.Application.DTOs;
using FluentValidation;

namespace Faturamento.Application.Validators;

public class ItemNotaRequestValidator : AbstractValidator<ItemNotaRequest>
{
    public ItemNotaRequestValidator()
    {
        RuleFor(x => x.CodigoProduto)
            .NotEmpty().WithMessage("O código do produto é obrigatório.")
            .MaximumLength(50).WithMessage("O código do produto não pode exceder 50 caracteres.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade do item deve ser maior que zero.")
            .LessThanOrEqualTo(1000000).WithMessage("A quantidade máxima permitida por item é de 1.000.000 unidades.");
    }
}

public class CriarNotaFiscalRequestValidator : AbstractValidator<CriarNotaFiscalRequest>
{
    public CriarNotaFiscalRequestValidator()
    {
        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A nota fiscal deve conter pelo menos um item.")
            .Must(itens => itens != null && itens.Count != 0).WithMessage("A lista de itens não pode estar vazia.")
            .Must(itens => itens.Count <= 100).WithMessage("Uma nota fiscal não pode conter mais de 100 itens.");

        // Aplica o validador de itens para cada item dentro da lista
        RuleForEach(x => x.Itens).SetValidator(new ItemNotaRequestValidator());
    }
}