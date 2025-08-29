using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class OpcNodeDtoValidator : AbstractValidator<OpcNodeDTO>
    {
        public OpcNodeDtoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100);

            RuleFor(x => x.NodeId)
                .NotEmpty().WithMessage("NodeId é obrigatório.")
                .MaximumLength(255);

            RuleFor(x => x.ServerId)
                .NotEmpty().WithMessage("ServerId é obrigatório.");
        }
    }
}
