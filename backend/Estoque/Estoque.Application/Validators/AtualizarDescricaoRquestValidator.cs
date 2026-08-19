using FluentValidation;
using Estoque.Application.DTOs;

namespace Estoque.Application.Validators;

public class AtualizarDescricaoRequestValidator : AbstractValidator<AtualizarDescricaoRequest>
{
    public AtualizarDescricaoRequestValidator()
    {
        RuleFor(x => x.NovaDescricao)
            .NotEmpty().WithMessage("A nova descrição não pode ser vazia.")
            .MaximumLength(255).WithMessage("A nova descrição não pode exceder 255 caracteres.")
            .Matches(@"^[^<>]*$").WithMessage("A descrição contém caracteres inválidos de formatação.");
    }
}