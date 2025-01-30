using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class UnidadeDtoValidator : AbstractValidator<UnidadeDto>
    {
        public UnidadeDtoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da unidade é obrigatório.");

        }
    }
}
