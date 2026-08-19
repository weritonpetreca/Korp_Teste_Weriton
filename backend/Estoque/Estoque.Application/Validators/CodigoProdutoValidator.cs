using FluentValidation;

namespace Estoque.Application.Validators;

public class CodigoProdutoValidator : AbstractValidator<string>
{
    public CodigoProdutoValidator()
    {
        RuleFor(codigo => codigo)
            .NotEmpty().WithMessage("O código do produto é obrigatório.")
            .MinimumLength(3).WithMessage("O código deve ter pelo menos 3 caracteres.")
            .MaximumLength(50).WithMessage("O código não pode exceder 50 caracteres.")
            .Matches("^[a-zA-Z0-9-]+$").WithMessage("O código contém caracteres inválidos. Apenas letras, números e hifens são permitidos.");
    }
}