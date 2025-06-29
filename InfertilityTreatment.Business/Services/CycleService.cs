using AutoMapper;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Notifications;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class CycleService : ICycleService
    {
        private readonly ITreatmentCycleRepository _treatmentCycleRepository;
        private readonly ITreatmentPhaseRepository _treatmentPhaseRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CycleService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IBaseRepository<User> _userRepository;

        public CycleService(
            ITreatmentCycleRepository treatmentCycleRepository,
            IMapper mapper,
            ITreatmentPhaseRepository treatmentPhaseRepository,
            IDoctorRepository doctorRepository,
            ILogger<CycleService> logger,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IBaseRepository<User> userRepository)
        {
            _treatmentCycleRepository = treatmentCycleRepository;
            _mapper = mapper;
            _treatmentPhaseRepository = treatmentPhaseRepository;
            _doctorRepository = doctorRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        public async Task<PhaseResponseDto> AddPhaseAsync(int cycleId, CreatePhaseDto createPhaseDto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                if (cycle == null)
                    throw new ArgumentException("Invalid cycle ID");

                var phase = _mapper.Map<TreatmentPhase>(createPhaseDto);
                phase.CycleId = cycleId;

                await _treatmentPhaseRepository.AddTreatmentPhaseAsync(phase);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<PhaseResponseDto>(phase);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }


        public async Task<bool> AssignDoctorToCycleAsync(int cycleId, int doctorId)
        {
            try
            {
                var doctor = await _doctorRepository.GetDoctorByIdAsync(doctorId);
                if (doctor is null)
                {
                    _logger.LogWarning("Doctor with ID {DoctorId} not found", doctorId);
                    throw new ArgumentException($"Doctor with ID {doctorId} does not exist");
                }

                if (!doctor.IsActive)
                {
                    _logger.LogWarning("Doctor with ID {DoctorId} is inactive", doctorId);
                    throw new InvalidOperationException("Cannot assign an inactive doctor to a treatment cycle");
                }

                var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                if (cycle is null)
                {
                    _logger.LogWarning($"Treatment cycle with ID {cycleId} not found", cycleId);
                    throw new ArgumentException($"Treatment cycle with ID {cycleId} does not exist");
                }

                if (cycle.Status == CycleStatus.Completed)
                {
                    _logger.LogWarning($"Cannot assign doctor to completed cycle {cycleId}", cycleId);
                    throw new InvalidOperationException("Cannot assign doctor to a completed treatment cycle");
                }

                var updated = await _treatmentCycleRepository.UpdateDoctorAsync(cycleId, doctorId);
                if (!updated)
                {
                    _logger.LogWarning($"Failed to assign doctor {doctorId} to cycle {cycleId}", doctorId, cycleId);
                    throw new InvalidOperationException("Failed to assign doctor to treatment cycle");
                }

                _logger.LogInformation($"Successfully assigned doctor {doctorId}  to cycle  {cycleId}", doctorId, cycleId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database error while assigning doctor {doctorId} to cycle {cycleId}", doctorId, cycleId);
                throw new InvalidOperationException("A database error occurred while assigning the doctor", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error assigning doctor {doctorId} to cycle {cycleId}", doctorId, cycleId);
                throw;
            }
        }

        public Task<decimal> CalculateCycleCostAsync(int cycleId)
        {
            return _treatmentCycleRepository.CalculatePhaseCostAsync(cycleId);
        }
        public async Task<CycleResponseDto> CreateCycleAsync(CreateCycleDto createCycleDto)
        {
            try
            {
                var cycle = await _treatmentCycleRepository.AddTreatmentCycleAsync(_mapper.Map<TreatmentCycle>(createCycleDto));

                if (cycle == null)
                {
                    throw new Exception("Unable to create TreatmentCycle.");
                }

                return _mapper.Map<CycleResponseDto>(cycle);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while creating the treatment cycle.", ex);
            }
        }
        public async Task<CycleDetailDto> GetCycleByIdAsync(int cycleId)
        {
            try
            {
                var cycle = await _treatmentCycleRepository.GetCycleByIdAsync(cycleId);

                if (cycle == null)
                {
                    throw new Exception("Unable to get TreatmentCycle.");
                }

                return _mapper.Map<CycleDetailDto>(cycle);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while creating the treatment cycle.", ex);
            }
        }
        public async Task<PaginatedResultDto<PhaseResponseDto>> GetCyclePhasesAsync(int cycleId, TreatmentPhaseFilterDto filter)
        {
            var pagedResult = await _treatmentPhaseRepository.GetCyclePhasesByCycleId(cycleId, filter);

            var mapped = _mapper.Map<List<PhaseResponseDto>>(pagedResult.Items);

            return new PaginatedResultDto<PhaseResponseDto>(
                mapped,
                pagedResult.TotalCount,
                pagedResult.PageNumber,
                pagedResult.PageSize
            );
        }

        public async Task<bool> UpdatePhaseAsync(int phaseId, UpdatePhaseDto updatePhaseDto)
        {
            var updated = await _treatmentPhaseRepository.UpdatePhaseAsync(
                phaseId,
                _mapper.Map<TreatmentPhase>(updatePhaseDto)
            );

            return updated;
        }
        public async Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByCustomerAsync(int customerId, TreatmentCycleFilterDto filter)
        {
            var paginatedResult = await _treatmentCycleRepository.GetCyclesByCustomerAsync(customerId, filter);

            var mappedItems = paginatedResult.Items
                .Select(c => _mapper.Map<CycleResponseDto>(c))
                .ToList();

            return new PaginatedResultDto<CycleResponseDto>(
                mappedItems,
                paginatedResult.TotalCount,
                paginatedResult.PageNumber,
                paginatedResult.PageSize
            );
        }

        public async Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByDoctorAsync(int doctorId, TreatmentCycleFilterDto filter)
        {
            var paginatedResult = await _treatmentCycleRepository.GetCyclesByDoctorAsync(doctorId, filter);

            var mappedItems = paginatedResult.Items
                .Select(c => _mapper.Map<CycleResponseDto>(c))
                .ToList();

            return new PaginatedResultDto<CycleResponseDto>(
                mappedItems,
                paginatedResult.TotalCount,
                paginatedResult.PageNumber,
                paginatedResult.PageSize
            );
        }

        public async Task<bool> UpdateCycleStatusAsync(int cycleId, CycleStatus status)
        {
            var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
            if (cycle == null)
            {
                _logger.LogWarning("Không tìm thấy Cycle với ID {CycleId} để cập nhật trạng thái.", cycleId);
                return false;
            }

            var oldStatus = cycle.Status;
            var updated = await _treatmentCycleRepository.UpdateStatusAsync(cycleId, status);

            if (updated)
            {
                // --- TRIGGER LOGIC FOR NOTIFICATIONS AND EMAILS ---
                try
                {
                    var customer = await _unitOfWork.Customers.GetByIdAsync(cycle.CustomerId);
                    if (customer != null)
                    {
                        var customerUser = await _userRepository.GetByIdAsync(customer.UserId);
                        if (customerUser != null)
                        {
                            var customerData = new Dictionary<string, string>
                            {
                                { "CustomerName", customerUser.FullName },
                                { "CycleId", cycle.Id.ToString() },
                                { "OldStatus", oldStatus.ToString() },
                                { "NewStatus", status.ToString() }
                            };

                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = customerUser.Id,
                                Title = $"Cập nhật trạng thái chu kỳ điều trị của bạn (ID: {cycle.Id})",
                                Message = $"Trạng thái chu kỳ điều trị của bạn đã thay đổi từ {oldStatus} sang {status}.",
                                Type = NotificationType.TreatmentCycleStatusChange,
                                RelatedEntityType = "TreatmentCycle",
                                RelatedEntityId = cycle.Id,
                                SendEmail = true,
                                EmailTemplateName = "TreatmentCycleStatusChange",
                                EmailTemplateData = customerData
                            });
                            _logger.LogInformation("Gửi thông báo thay đổi trạng thái chu kỳ điều trị cho Customer {CustomerId}.", customer.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo thông báo cho thay đổi trạng thái chu kỳ {CycleId}.", cycleId);
                }
            }
            return updated;
        }

        public Task<bool> UpdateCycleAsync(int cycleId, UpdateCycleDto dto)
        {
            var cycle = _mapper.Map<TreatmentCycle>(dto);
            cycle.Id = cycleId;
            return _treatmentCycleRepository.UpdateTreatmentCycleAsync(cycle);
        }
    }
}
