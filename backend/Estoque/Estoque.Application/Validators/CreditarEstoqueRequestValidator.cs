using FluentValidation;
using Estoque.Application.DTOs;

namespace Estoque.Application.Validators;

public class CreditarEstoqueRequestValidator : AbstractValidator<CreditarEstoqueRequest>
{
    public CreditarEstoqueRequestValidator()
    {
        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade a ser creditada deve ser maior que zero.")
            .LessThanOrEqualTo(100000).WithMessage("A quantidade máxima permitida por operação de crédito é 100.000 unidades.");
    }
}