using FluentValidation;
using InfertilityTreatment.Entity.DTOs.TestResultss;
using System;

namespace InfertilityTreatment.Business.Validators
{
    public class CreateTestResultValidator : AbstractValidator<CreateTestResultDto>
    {
        public CreateTestResultValidator()
        {
            RuleFor(x => x.CycleId).GreaterThan(0);
            RuleFor(x => x.TestType).IsInEnum();
            RuleFor(x => x.TestDate).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Test date cannot be in the future");
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Results).NotNull();
            // Add more business rules as needed
        }
    }
}
