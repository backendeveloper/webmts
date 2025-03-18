using AuthService.Contract.Requests;
using FluentValidation;

namespace AuthService.Client.InputValidators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı gereklidir.")
            .Length(3, 50).WithMessage("Kullanıcı adı 3-50 karakter arasında olmalıdır.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
        
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Şifre onayı gereklidir.")
            .Equal(x => x.Password).WithMessage("Şifreler eşleşmiyor.");
            
        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roller listesi null olamaz.");
    }
}