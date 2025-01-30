using FluentValidation;
using APsiControleApi.Application.DTOs;
using BrazilianUtils;

namespace APsiControleApi.Application.Validators
{
    public class EmpresaDTOValidator : AbstractValidator<EmpresaDTO>
    {
        public EmpresaDTOValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da empresa é obrigatório.")
                .MaximumLength(100).WithMessage("O nome da empresa não pode exceder 100 caracteres.");
            
            RuleFor(x => x.Cnpj)
                .NotEmpty().WithMessage("O CNPJ é obrigatório.")
                .Must(Cnpj.IsValid).WithMessage("O CNPJ fornecido é inválido.");

            RuleFor(x => x.Endereco)
                .NotEmpty().WithMessage("O endereço é obrigatório.");
            
            RuleFor(x => x.Regiao)
                .NotEmpty().WithMessage("A região é obrigatória.");
        }
    }
}
