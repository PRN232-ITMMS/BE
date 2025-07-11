using FluentValidation;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Business.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required")
                .MaximumLength(100)
                .WithMessage("Full name cannot exceed 100 characters");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[\d\-\+\(\)\s]+$")
                .WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.Role)
                .NotEqual(UserRole.Customer)
                .WithMessage("Customer role is not allowed for this endpoint")
                .NotEqual(UserRole.Admin)
                .WithMessage("Admin role is not allowed for this endpoint")
                .Must(role => role == UserRole.Doctor || role == UserRole.Manager)
                .WithMessage("Only Doctor or Manager roles are allowed");

            // Doctor-specific validations
            When(x => x.Role == UserRole.Doctor, () =>
            {
                RuleFor(x => x.LicenseNumber)
                    .NotEmpty()
                    .WithMessage("License number is required for Doctor role")
                    .MaximumLength(100)
                    .WithMessage("License number cannot exceed 100 characters");

                RuleFor(x => x.Specialization)
                    .NotEmpty()
                    .WithMessage("Specialization is required for Doctor role")
                    .MaximumLength(200)
                    .WithMessage("Specialization cannot exceed 200 characters");

                RuleFor(x => x.YearsOfExperience)
                    .NotNull()
                    .WithMessage("Years of experience is required for Doctor role")
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Years of experience must be a positive number")
                    .LessThanOrEqualTo(70)
                    .WithMessage("Years of experience cannot exceed 70 years");

                RuleFor(x => x.Education)
                    .MaximumLength(500)
                    .WithMessage("Education cannot exceed 500 characters")
                    .When(x => !string.IsNullOrEmpty(x.Education));

                RuleFor(x => x.Biography)
                    .MaximumLength(2000)
                    .WithMessage("Biography cannot exceed 2000 characters")
                    .When(x => !string.IsNullOrEmpty(x.Biography));

                RuleFor(x => x.ConsultationFee)
                    .GreaterThan(0)
                    .WithMessage("Consultation fee must be greater than 0")
                    .LessThanOrEqualTo(999999.99m)
                    .WithMessage("Consultation fee cannot exceed 999,999.99")
                    .When(x => x.ConsultationFee.HasValue);

                RuleFor(x => x.SuccessRate)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Success rate must be between 0 and 100")
                    .LessThanOrEqualTo(100)
                    .WithMessage("Success rate must be between 0 and 100")
                    .When(x => x.SuccessRate.HasValue);
            });
        }
    }
}
