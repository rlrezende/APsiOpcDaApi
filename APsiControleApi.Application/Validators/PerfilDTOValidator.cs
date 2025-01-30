using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class PerfilDTOValidator : AbstractValidator<PerfilDTO>
    {
        public PerfilDTOValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome do perfil é obrigatório.");

       //     RuleFor(x => x.ModuloIds)
       //         .NotEmpty().WithMessage("O perfil deve estar associado a pelo menos um módulo.");
        }
    }
}
