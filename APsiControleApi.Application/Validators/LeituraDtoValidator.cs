using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class LeituraDtoValidator : AbstractValidator<LeituraDTO>
    {
        public LeituraDtoValidator()
        {
            // Validação para o campo DataLeitura (não pode ser uma data futura)
            RuleFor(x => x.DataLeitura)
                .NotEmpty().WithMessage("A data da leitura é obrigatória.")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("A data da leitura não pode estar no futuro.");

            // Validação para o campo Valor (deve ser maior ou igual a zero)
            RuleFor(x => x.Valor)
                .GreaterThanOrEqualTo(0).WithMessage("O valor da leitura não pode ser negativo.");

            // Validação para TagId (deve ser obrigatório)
            RuleFor(x => x.TagId)
                .NotEmpty().WithMessage("O identificador da tag é obrigatório.");

        }
    }
}
