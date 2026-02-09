using FluentValidation;

namespace FinNex.Application.Validators.Base
{
    public class BaseValidator<T> : AbstractValidator<T>
    {
        // Bura bütün DTO-lar üçün ortaq olan məntiqləri yaza bilərsən
    }
}
