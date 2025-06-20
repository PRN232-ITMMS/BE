using FluentValidation;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Data.Repositories.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Validators
{
    public class CreateCycleDtoValidator : AbstractValidator<CreateCycleDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly ITreatmentPackageRepository _packageRepository;
        private readonly ITreatmentCycleRepository _cycleRepository;

        public CreateCycleDtoValidator(
            ICustomerRepository customerRepository,
            IDoctorRepository doctorRepository,
            ITreatmentPackageRepository packageRepository,
            ITreatmentCycleRepository cycleRepository)
        {
            _customerRepository = customerRepository;
            _doctorRepository = doctorRepository;
            _packageRepository = packageRepository;
            _cycleRepository = cycleRepository;

            RuleFor(x => x.CustomerId)
                .MustAsync(CustomerExists).WithMessage("Customer does not exist");

            RuleFor(x => x.DoctorId)
                .MustAsync(DoctorAvailable).WithMessage("Doctor is inactive or not found");

            RuleFor(x => x.PackageId)
                .MustAsync(PackageActive).WithMessage("Treatment package is not available");

            RuleFor(x => x.ExpectedEndDate)
                .GreaterThan(x => x.StartDate.Value)
                .When(x => x.StartDate.HasValue)
                .WithMessage("Expected end date must be after start date");
        }

        private async Task<bool> CustomerExists(int customerId, CancellationToken ct)
        {
            return await _customerRepository.ExistsAsync(customerId);
        }

        private async Task<bool> DoctorAvailable(int doctorId, CancellationToken ct)
        {
            var doctor = await _doctorRepository.GetDoctorByIdAsync(doctorId);
            return doctor != null && doctor.IsActive;
        }

        private async Task<bool> PackageActive(int packageId, CancellationToken ct)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);
            return package != null && package.IsActive;
        }
    }
}
