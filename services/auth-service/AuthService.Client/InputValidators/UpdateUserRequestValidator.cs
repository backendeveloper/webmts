using AuthService.Contract.Requests;
using FluentValidation;

namespace AuthService.Client.InputValidators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");
            
        RuleFor(x => x.Username)
            .Length(3, 50).WithMessage("Kullanıcı adı 3-50 karakter arasında olmalıdır.")
            .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .When(x => !string.IsNullOrEmpty(x.Password));
            
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Şifreler eşleşmiyor.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}