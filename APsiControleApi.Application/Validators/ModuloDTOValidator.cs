using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class ModuloDTOValidator : AbstractValidator<ModuloDTO>
    {
        public ModuloDTOValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome do módulo é obrigatório.");

            RuleFor(x => x.PerfilIds)
                .NotEmpty().WithMessage("O módulo deve estar associado a pelo menos um perfil.");
        }
    }
}
