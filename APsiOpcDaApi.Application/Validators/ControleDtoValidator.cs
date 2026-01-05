using FluentValidation;
using APsiOpcDaApi.Application.DTOs;

namespace APsiOpcDaApi.Application.Validators
{
    public class ControleDtoValidator : AbstractValidator<ControleDTO>
    {
        public ControleDtoValidator()
        {
            // Validação para o campo Nome
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome do controle é obrigatório.")
                .MaximumLength(100).WithMessage("O nome do controle não pode ter mais de 100 caracteres.");

            // Validação para o campo Descricao (opcional, mas com limite)
            RuleFor(x => x.Descricao)
                .MaximumLength(250).WithMessage("A descrição do controle não pode ter mais de 250 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Descricao));

            // Validação para UnidadeId (obrigatório)
            RuleFor(x => x.UnidadeId)
                .NotEmpty().WithMessage("O identificador da unidade é obrigatório.");

            // Validação para UnidadeNome (opcional, mas com limite)
            RuleFor(x => x.UnidadeNome)
                .MaximumLength(100).WithMessage("O nome da unidade não pode ter mais de 100 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.UnidadeNome));
        }
    }
}

