using FluentValidation;
using Estoque.Domain;

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
            .MaximumLength(255).WithMessage("O descrição do produto não pode exceder 255 caracteres.")
            .Matches(@"^[^<>]*$").WithMessage("A descrição contém caracteres inválidos de formatação.");

        // Regras para o Saldo (Limites Operacionais)
        RuleFor(p => p.Saldo)
            .GreaterThanOrEqualTo(0).WithMessage("O saldo inicial não pode ser negativo.")
            .LessThanOrEqualTo(999999).WithMessage("O saldo inicial não pode exceder 999.999 unidades.");

        // Validação opcional de formato para garantir que a auditoria interna seja sempre uma data ISO 8601 válida
        RuleFor(p => p.DataCriacao)
            .NotEmpty().WithMessage("A data de criação é obrigatória.")
            .Must(BeAValidIso8601Date).WithMessage("A data de criação deve estar no formato ISO 8601 válido.");

        RuleFor(p => p.DataAtualizacao)
            .NotEmpty().WithMessage("A data de atualização é obrigatória.")
            .Must(BeAValidIso8601Date).WithMessage("A data de atualização deve estar no formato ISO 8601 válido.");
    }

    private bool BeAValidIso8601Date(string dateStr)
    {
        return DateTime.TryParse(dateStr, out _);
    }
}