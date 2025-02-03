using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class TagDtoValidator : AbstractValidator<TagDTO>
    {
        public TagDtoValidator()
        {
            // Validação para o campo Nome
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da tag é obrigatório.")
                .MaximumLength(100).WithMessage("O nome da tag não pode ter mais de 100 caracteres.");

            // Validação para o campo Descricao (opcional ou limitado)
            RuleFor(x => x.Descricao)
                .MaximumLength(250).WithMessage("A descrição da tag não pode ter mais de 250 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Descricao));

         
        }
    }
}
