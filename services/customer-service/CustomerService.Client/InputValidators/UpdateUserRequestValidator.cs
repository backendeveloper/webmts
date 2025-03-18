using CustomerService.Contract.Requests;
using FluentValidation;

namespace CustomerService.Client.InputValidators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");
            
        RuleFor(x => x.Username)
            .Length(3, 50).WithMessage("Kullanıcı adı 3-50 karakter arasında olmalıdır.")
            .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}