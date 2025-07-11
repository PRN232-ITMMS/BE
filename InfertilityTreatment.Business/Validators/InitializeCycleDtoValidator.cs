using FluentValidation;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using System;

namespace InfertilityTreatment.Business.Validators
{
    public class InitializeCycleDtoValidator : AbstractValidator<InitializeCycleDto>
    {
        public InitializeCycleDtoValidator()
        {
            RuleFor(x => x.TreatmentPlan)
                .NotEmpty()
                .WithMessage("Treatment plan is required")
                .MaximumLength(2000)
                .WithMessage("Treatment plan cannot exceed 2000 characters");

            RuleFor(x => x.EstimatedCompletionDate)
                .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
                .WithMessage("Estimated completion date must be in the future");

            RuleFor(x => x.SpecialInstructions)
                .MaximumLength(1000)
                .WithMessage("Special instructions cannot exceed 1000 characters");

           
        }
    }
}
