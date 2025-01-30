using FluentValidation;
using APsiControleApi.Application.DTOs;

namespace APsiControleApi.Application.Validators
{
    public class CriarLicencaRequestDTOValidator : AbstractValidator<CriarLicencaRequest>
    {
        public CriarLicencaRequestDTOValidator()
        {
            RuleFor(x => x.Empresa).SetValidator(new EmpresaDTOValidator());
            RuleFor(x => x.UsuarioRoot).SetValidator(new UsuarioDtoValidator());
            RuleFor(x => x.Licenca).SetValidator(new LicencaDTOValidator());
        }
    }
}
