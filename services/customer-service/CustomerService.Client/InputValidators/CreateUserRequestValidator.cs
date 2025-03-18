using CustomerService.Contract.Requests;
using FluentValidation;

namespace CustomerService.Client.InputValidators;

public class CreateUserRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı gereklidir.")
            .Length(3, 50).WithMessage("Kullanıcı adı 3-50 karakter arasında olmalıdır.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}