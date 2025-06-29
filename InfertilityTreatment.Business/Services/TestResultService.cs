using InfertilityTreatment.Entity.Enums;
using InfertilityTreatment.Entity.Entities;

using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Results;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Notifications;
using Microsoft.Extensions.Logging;

namespace InfertilityTreatment.Business.Services
{
    public class TestResultService : ITestResultService
    {
        private readonly ITestResultRepository _repo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ITreatmentCycleRepository _cycleRepo;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IBaseRepository<User> _userRepository;
        private readonly IBaseRepository<Doctor> _doctorRepository;
        private readonly IBaseRepository<Customer> _customerRepository;
        private readonly ILogger<TestResultService> _logger;

        public TestResultService(
            ITestResultRepository repo,
            IAppointmentRepository appointmentRepo,
            ITreatmentCycleRepository cycleRepo,
            IMapper mapper,
            INotificationService notificationService,
            IBaseRepository<User> userRepository,
            IBaseRepository<Doctor> doctorRepository,
            IBaseRepository<Customer> customerRepository,
            ILogger<TestResultService> logger)
        {
            _repo = repo;
            _appointmentRepo = appointmentRepo;
            _cycleRepo = cycleRepo;
            _mapper = mapper;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _doctorRepository = doctorRepository;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<TestResultDto> CreateTestResultAsync(CreateTestResultDto dto)
        {
            await ValidateTestResultDtoAsync(dto);
            var entity = _mapper.Map<TestResult>(dto);
            var created = await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            // --- TRIGGER LOGIC FOR NOTIFICATIONS AND EMAILS ---
            try
            {
                var cycle = await _cycleRepo.GetByIdAsync(created.CycleId);
                if (cycle == null)
                {
                    _logger.LogWarning("Treatment cycle with ID {CycleId} not found for test result {TestResultId}. Skipping notifications.", created.CycleId, created.Id);
                    return _mapper.Map<TestResultDto>(created);
                }

                var customer = await _customerRepository.GetByIdAsync(cycle.CustomerId);
                var customerUser = (customer != null) ? await _userRepository.GetByIdAsync(customer.UserId) : null;


                if (customerUser != null && !string.IsNullOrEmpty(customerUser.Email))
                {
                    var customerData = new Dictionary<string, string>
                    {
                        { "CustomerName", customerUser.FullName },
                        { "TestType", created.TestType.ToString() }
                    };
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = customerUser.Id,
                        Title = "Kết quả xét nghiệm của bạn đã có",
                        Message = $"Kết quả xét nghiệm loại {created.TestType} của bạn đã có sẵn. Vui lòng kiểm tra.",
                        Type = NotificationType.Result,
                        RelatedEntityType = "TestResult",
                        RelatedEntityId = created.Id,
                        SendEmail = true,
                        EmailTemplateName = "TestResultsAvailable",
                        EmailTemplateData = customerData
                    });
                    _logger.LogInformation("Gửi thông báo kết quả xét nghiệm cho khách hàng {UserId}.", customerUser.Id);

                    if (dto.IsCritical)
                    {
                        var doctor = await _doctorRepository.GetByIdAsync(cycle.DoctorId);
                        if (doctor != null)
                        {
                            var doctorUser = await _userRepository.GetByIdAsync(doctor.UserId);
                            if (doctorUser != null && !string.IsNullOrEmpty(doctorUser.Email))
                            {
                                var doctorData = new Dictionary<string, string>
                                {
                                    { "DoctorName", doctorUser.FullName },
                                    { "CustomerName", customerUser.FullName },
                                    { "CycleId", created.CycleId.ToString() },
                                    { "TestType", created.TestType.ToString() },
                                    { "TestResults", created.Results ?? "N/A" },
                                    { "ReferenceRange", created.ReferenceRange ?? "N/A" }
                                };
                                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                                {
                                    UserId = doctorUser.Id,
                                    Title = "[KHẨN CẤP] Kết quả xét nghiệm quan trọng",
                                    Message = $"Một kết quả xét nghiệm quan trọng của bệnh nhân {customerUser.FullName} (Cycle ID: {created.CycleId}) đã được ghi nhận. Loại xét nghiệm: {created.TestType}.",
                                    Type = NotificationType.CriticalTestResult,
                                    RelatedEntityType = "TestResult",
                                    RelatedEntityId = created.Id,
                                    SendEmail = true,
                                    EmailTemplateName = "CriticalTestResultAlert",
                                    EmailTemplateData = doctorData
                                });
                                _logger.LogInformation("Gửi cảnh báo kết quả xét nghiệm KHẨN CẤP cho bác sĩ {DoctorId}.", doctor.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo cho kết quả xét nghiệm mới {TestResultId}.", created.Id);
            }

            return _mapper.Map<TestResultDto>(created);
        }

        private async Task ValidateTestResultDtoAsync(CreateTestResultDto dto)
        {
            if (dto.TestDate > DateTime.UtcNow)
                throw new ArgumentException("Test date cannot be in the future");

            var cycle = await _cycleRepo.GetByIdAsync(dto.CycleId);
            if (cycle == null)
                throw new ArgumentException($"Treatment cycle with id {dto.CycleId} does not exist");
            var apm = await _appointmentRepo.GetByIdAsync(dto.AppointmentId);
            if (apm == null)
                throw new ArgumentException($"Appointment with id {dto.AppointmentId} does not exist");

            if (!Enum.IsDefined(typeof(TestResultType), dto.TestType))
                throw new ArgumentException($"TestType '{dto.TestType}' is not valid");

            if (!Enum.IsDefined(typeof(TestResultStatus), dto.Status))
                throw new ArgumentException($"Status '{dto.Status}' is not valid");
        }

        private async Task ValidateUpdateTestResultDtoAsync(UpdateTestResultDto dto)
        {
            if (dto.TestDate > DateTime.UtcNow)
                throw new ArgumentException("Test date cannot be in the future");

            var cycle = await _cycleRepo.GetByIdAsync(dto.CycleId);
            if (cycle == null)
                throw new ArgumentException($"Treatment cycle with id {dto.CycleId} does not exist");
            var apm = await _appointmentRepo.GetByIdAsync(dto.AppointmentId);
            if (apm == null)
                throw new ArgumentException($"Appointment with id {dto.AppointmentId} does not exist");

            if (!Enum.IsDefined(typeof(TestResultType), dto.TestType))
                throw new ArgumentException($"TestType '{dto.TestType}' is not valid");

            if (!Enum.IsDefined(typeof(TestResultStatus), dto.Status))
                throw new ArgumentException($"Status '{dto.Status}' is not valid");
        }

        public async Task<PaginatedResultDto<TestResultDto>> GetTestResultsByCycleAsync(int cycleId, PaginationQueryDTO pagination)
        {
            var paged = await _repo.GetTestResultsByCycleAsync(cycleId, pagination);
            return new PaginatedResultDto<TestResultDto>(
                _mapper.Map<List<TestResultDto>>(paged.Items),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
        }

        public async Task<TestResultDetailDto?> GetTestResultByIdAsync(int testResultId)
        {
            var entity = await _repo.GetByIdAsync(testResultId);
            if (entity == null) return null;
            var detailDto = _mapper.Map<TestResultDetailDto>(entity);
            detailDto.ResultInterpretation = InterpretResult(entity);
            return detailDto;
        }

        private string InterpretResult(TestResult entity)
        {
            if (!decimal.TryParse(entity.Results, out var resultValue))
                return "RequiresAttention";

            if (TryParseRange(entity.ReferenceRange, out var min, out var max, out var type))
            {
                switch (type)
                {
                    case RangeType.MinMax:
                        if (resultValue < min * 0.8m)
                            return "CriticalLow";
                        if (resultValue < min)
                            return "Low";
                        if (resultValue > max * 1.2m)
                            return "CriticalHigh";
                        if (resultValue > max)
                            return "High";
                        return "Normal";

                    case RangeType.GreaterThan:
                        if (resultValue <= min * 0.8m)
                            return "CriticalLow";
                        if (resultValue <= min)
                            return "Low";
                        return "Normal";

                    case RangeType.LessThan:
                        if (resultValue >= max * 1.2m)
                            return "CriticalHigh";
                        if (resultValue >= max)
                            return "High";
                        return "Normal";
                }
            }

            return "RequiresAttention";
        }

        private bool TryParseRange(string? range, out decimal min, out decimal max, out RangeType type)
        {
            min = max = 0;
            type = RangeType.MinMax;

            if (string.IsNullOrWhiteSpace(range)) return false;

            range = range.Trim();

            if (range.Contains('-'))
            {
                var parts = range.Split('-', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 &&
                    decimal.TryParse(parts[0], out min) &&
                    decimal.TryParse(parts[1], out max))
                {
                    type = RangeType.MinMax;
                    return true;
                }
            }
            else if (range.StartsWith(">") && decimal.TryParse(range[1..], out min))
            {
                type = RangeType.GreaterThan;
                return true;
            }
            else if (range.StartsWith("<") && decimal.TryParse(range[1..], out max))
            {
                type = RangeType.LessThan;
                return true;
            }

            return false;
        }

        public async Task<TestResultDto> UpdateTestResultAsync(int testResultId, UpdateTestResultDto dto)
        {
            await ValidateUpdateTestResultDtoAsync(dto);
            var entity = await _repo.GetByIdAsync(testResultId);
            if (entity == null) throw new KeyNotFoundException("TestResult not found");
            _mapper.Map(dto, entity);
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<TestResultDto>(entity);
        }

        public async Task<PaginatedResultDto<TestResultDto>> GetTestResultsByTypeAsync(int cycleId, TestResultType type, PaginationQueryDTO pagination)
        {
            var paged = await _repo.GetTestResultsByTypeAsync(cycleId, type, pagination);
            return new PaginatedResultDto<TestResultDto>(
                _mapper.Map<List<TestResultDto>>(paged.Items),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
        }

        public async Task<PaginatedResultDto<TestResultDto>> GetTestResultsAsync(int? cycleId, TestResultType? type, DateTime? date, PaginationQueryDTO pagination)
        {
            var paged = await _repo.GetTestResultsAsync(cycleId, type, date, pagination);
            return new PaginatedResultDto<TestResultDto>(
                _mapper.Map<List<TestResultDto>>(paged.Items),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
        }
    }
}