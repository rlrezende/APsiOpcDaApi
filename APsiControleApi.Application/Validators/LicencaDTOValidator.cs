using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class LicencaDTOValidator : AbstractValidator<LicencaDTO>
    {
        public LicencaDTOValidator()
        {
            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("A data de início é obrigatória.");

            RuleFor(x => x.DataFim)
                .NotEmpty().WithMessage("A data de fim é obrigatória.")
                .GreaterThanOrEqualTo(x => x.DataInicio).WithMessage("A data de fim deve ser posterior à data de início.");
            
            RuleFor(x => x.ModuloIds)
                .NotEmpty().WithMessage("Deve haver ao menos um módulo associado.");
        }
    }
}
