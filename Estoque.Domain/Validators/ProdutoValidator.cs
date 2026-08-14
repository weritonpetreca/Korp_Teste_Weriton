using FluentValidation;

namespace Estoque.Domain.Validators;

public class ProdutoValidator : AbstractValidator<Produto>
{
    public ProdutoValidator()
    {
        // Regras para o Código (Strict Whitelisting)
        RuleFor(p => p.Codigo)
            .NotEmpty().WithMessage("O código do produto não pode ser vazio.")
            .MaximumLength(50).WithMessage("O código do produto não pode exceder 50 caracteres.")
            .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("O código do produto deve conter apenas letras, números e hifens.");

        // Regras para a Descrição (Defesa em Profundidade contra XSS)
        RuleFor(p => p.Descricao)
            .NotEmpty().WithMessage("A descrição do produto não pode ser vazia.")
            .MaximumLength(255).WithMessage("A descrição do produto não pode exceder 255 caracteres.")
            // Bloqueia as tags < e > que são a base de exploits XSS e Injeção de HTML
            .Matches(@"^[^<>]*$").WithMessage("A descrição contém caracteres inválidos de formatação.");

        // Regras para o Saldo (Limites Operacionais)
        RuleFor(p => p.Saldo)
            .GreaterThanOrEqualTo(0).WithMessage("O saldo inicial não pode ser negativo.")
            .LessThanOrEqualTo(999999).WithMessage("O saldo inicial não pode exceder 999.999 unidades.");
    }
}