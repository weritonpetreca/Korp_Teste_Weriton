using FluentValidation;
using Estoque.Application.DTOs;

namespace Estoque.Application.Validators;

public class DebitarEstoqueRequestValidator : AbstractValidator<DebitarEstoqueRequest>
{
    public DebitarEstoqueRequestValidator()
    {
        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade a ser debitada deve ser maior que zero.")
            .LessThanOrEqualTo(10000).WithMessage("A quantidade máxima permitida por operação de débito é 10.000 unidades.");
    }
}