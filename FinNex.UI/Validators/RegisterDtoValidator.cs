using FinNex.UI.DTO;
using FluentValidation;

namespace FinNex.UI.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Ad)
                .NotEmpty().WithMessage("Ad boş ola bilməz")
                .MinimumLength(2).WithMessage("Ad ən azı 2 simvol olmalıdır");

            RuleFor(x => x.Soyad)
                .NotEmpty().WithMessage("Soyad boş ola bilməz")
                .MinimumLength(2).WithMessage("Soyad ən azı 2 simvol olmalıdır");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email boş ola bilməz")
                .EmailAddress().WithMessage("Email formatı yanlışdır");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifrə boş ola bilməz")
                .MinimumLength(8).WithMessage("Şifrə ən azı 8 simvol olmalıdır")
                .Matches("[A-Z]").WithMessage("Şifrədə ən azı 1 böyük hərf olmalıdır")
                .Matches("[a-z]").WithMessage("Şifrədə ən azı 1 kiçik hərf olmalıdır")
                .Matches("[0-9]").WithMessage("Şifrədə ən azı 1 rəqəm olmalıdır");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Şifrələr eyni deyil");
        }
    }
}
