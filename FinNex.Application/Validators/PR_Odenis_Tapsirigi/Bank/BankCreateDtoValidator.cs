using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FluentValidation;

namespace FinNex.Application.Validators.PR_Odenis_Tapsirigi.Bank
{
    public class BankCreateDtoValidator : AbstractValidator<BankCreateDto>
    {
        public BankCreateDtoValidator()
        {
            RuleFor(x => x.Ad)
                .NotEmpty().WithMessage("Bank adı mütləq daxil edilməlidir.")
                .MaximumLength(200).WithMessage("Bank adı 200 simvoldan çox ola bilməz.");

            RuleFor(x => x.SwiftBic)
                .NotEmpty().WithMessage("SWIFT kodu boş ola bilməz.")
                .Length(8, 11).WithMessage("SWIFT kodu 8 və ya 11 simvol olmalıdır.");

            RuleFor(x => x.Voen)
                .NotEmpty().WithMessage("VOEN boş ola bilməz.")
                .Length(8, 11).WithMessage("VOEN kodu 8 və ya 11 simvol olmalıdır.");

            RuleFor(x => x.MuxHesab)
                .NotEmpty().WithMessage("Muxbir hesab boş ola bilməz.");
        }
    }
}
