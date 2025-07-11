using AutoMapper;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
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
        private readonly ITestResultRepository _testResultRepository;
        private readonly IDoctorScheduleRepository _doctorScheduleRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CycleService> _logger;
        
        public CycleService(
            ITreatmentCycleRepository treatmentCycleRepository, 
            IMapper mapper, 
            ITreatmentPhaseRepository treatmentPhaseRepository, 
            IDoctorRepository doctorRepository, 
            ITestResultRepository testResultRepository,
            IDoctorScheduleRepository doctorScheduleRepository,
            IAppointmentRepository appointmentRepository,
            ILogger<CycleService> logger, 
            IUnitOfWork unitOfWork)
        {
            _treatmentCycleRepository = treatmentCycleRepository;
            _mapper = mapper;
            _treatmentPhaseRepository = treatmentPhaseRepository;
            _doctorRepository = doctorRepository;
            _testResultRepository = testResultRepository;
            _doctorScheduleRepository = doctorScheduleRepository;
            _appointmentRepository = appointmentRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
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
                // 1. Validate doctor
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

                // 2. Validate treatment cycle
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

                // 3. Assign doctor
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
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate Package
                var package = await _unitOfWork.TreatmentPackages.GetByIdAsync(createCycleDto.PackageId);
                if (package == null || !package.IsActive)
                    throw new ArgumentException($"Treatment package with ID {createCycleDto.PackageId} does not exist or is inactive");

                // Validate Doctor
                    var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(createCycleDto.DoctorId);
                    if (doctor == null || !doctor.IsActive || !doctor.IsAvailable)
                        throw new ArgumentException($"Doctor with ID {createCycleDto.DoctorId} does not exist, is inactive, or not available");

                // Validate CycleNumber uniqueness
                var existingCycle = await _treatmentCycleRepository.GetCycleByCustomerAndNumberAsync(
                    createCycleDto.CustomerId, createCycleDto.CycleNumber);
                if (existingCycle != null)
                    throw new ArgumentException($"Cycle number {createCycleDto.CycleNumber} already exists for this customer");

                // Validate date logic
                ValidateDateLogic(createCycleDto);

                var cycle = await _treatmentCycleRepository.AddTreatmentCycleAsync(_mapper.Map<TreatmentCycle>(createCycleDto));
                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<CycleResponseDto>(cycle);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        public async Task<CycleDetailDto> GetCycleByIdAsync(int cycleId)
        {
            try
            {
                var cycle = await _treatmentCycleRepository.GetCycleByIdAsync(cycleId);

                if (cycle == null)
                {
                    throw new InvalidOperationException("Unable to get TreatmentCycle.");
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
        public async Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByCustomerAsync(int userId, TreatmentCycleFilterDto filter)
        {
            var customer = await _unitOfWork.Customers.GetByUserIdAsync(userId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer with ID {userId} does not exist");
            }
            var paginatedResult = await _treatmentCycleRepository.GetCyclesByCustomerAsync(customer.Id, filter);

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

        public async Task<PaginatedResultDto<CycleResponseDto>> GetCyclesByDoctorAsync(int userId, TreatmentCycleFilterDto filter)
        {
            var doctor = await _unitOfWork.Doctors.GetByUserIdAsync(userId);
            if (doctor == null)
            {
                throw new ArgumentException($"Doctor with ID {userId} does not exist");
            }
            var paginatedResult = await _treatmentCycleRepository.GetCyclesByDoctorAsync(doctor.Id, filter);

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

        Task<bool> ICycleService.UpdateCycleStatusAsync(int cycleId, CycleStatus status)
        {
            return _treatmentCycleRepository.UpdateStatusAsync(cycleId, status);
        }
        public Task<bool> UpdateCycleAsync(int cycleId, UpdateCycleDto dto)
        {
            var cycle = _mapper.Map<TreatmentCycle>(dto);
            cycle.Id = cycleId;
            return _treatmentCycleRepository.UpdateTreatmentCycleAsync(cycle);
        }

        private void ValidateDateLogic(CreateCycleDto dto)
        {
            if (dto.StartDate.HasValue && dto.ExpectedEndDate.HasValue)
            {
                if (dto.StartDate >= dto.ExpectedEndDate)
                    throw new ArgumentException("StartDate must be before ExpectedEndDate");
            }

            if (dto.ActualEndDate.HasValue && dto.StartDate.HasValue)
            {
                if (dto.ActualEndDate <= dto.StartDate)
                    throw new ArgumentException("ActualEndDate must be after StartDate");
            }

            if (dto.ExpectedEndDate.HasValue)
            {
                if (dto.ExpectedEndDate < DateTime.UtcNow.Date)
                    throw new ArgumentException("ExpectedEndDate cannot be in the past");
                if (dto.ExpectedEndDate > DateTime.UtcNow.AddYears(2))
                    throw new ArgumentException("ExpectedEndDate is too far in the future");
            }
        }

        #region Phase Management Methods BE-022

        public async Task<PhaseResponseDto> StartPhaseAsync(int cycleId, int phaseId, StartPhaseDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate cycle exists
                var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                if (cycle == null)
                    throw new ArgumentException("Cycle ID is invalid");

                // Get the phase and validate it belongs to the cycle
                var phase = await _treatmentPhaseRepository.GetTreatmentPhaseByIdAsync(phaseId);
                if (phase == null || phase.CycleId != cycleId)
                    throw new ArgumentException("Invalid phase ID or phase does not belong to the specified cycle");

                // Validate phase can be started
                if (phase.Status != PhaseStatus.Pending)
                    throw new InvalidOperationException($"Phase cannot be started. Current status: {phase.Status}");

                // Check if previous phases are completed (if any): validate phase phai theo thu tu, khong lam 2 truoc 1
                var previousPhases = await _treatmentPhaseRepository.GetPhasesByCycleIdAsync(cycleId);
                var incompletePreviousPhases = previousPhases
                    .Where(p => p.PhaseOrder < phase.PhaseOrder && p.Status != PhaseStatus.Completed)
                    .Any();

                if (incompletePreviousPhases)
                    throw new InvalidOperationException("Previous phases must be completed before starting this phase");

                // Update phase
                phase.Status = PhaseStatus.InProgress;
                phase.StartDate = dto.StartDate;
                if (!string.IsNullOrEmpty(dto.Instructions))
                    phase.Instructions = dto.Instructions;
                if (!string.IsNullOrEmpty(dto.Notes))
                    phase.Notes = dto.Notes;
                phase.UpdatedAt = DateTime.UtcNow;

                await _treatmentPhaseRepository.UpdateTreatmentPhaseAsync(phase);

                // Update TreatmentCycle status to InProgress if phase start is successful
                if (cycle.Status != CycleStatus.InProgress)
                {
                    // Use dedicated status update method to ensure database is updated
                    var statusUpdated = await _treatmentCycleRepository.UpdateStatusAsync(cycleId, CycleStatus.InProgress);
                    
                    if (statusUpdated)
                    {
                        _logger.LogInformation("Cycle {CycleId} status updated to InProgress (3) during phase start", cycleId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update cycle {CycleId} status to InProgress", cycleId);
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Phase {phaseId} started for cycle {cycleId}");
                return _mapper.Map<PhaseResponseDto>(phase);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PhaseResponseDto> CompletePhaseAsync(int cycleId, int phaseId, CompletePhaseDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate cycle exists
                var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                if (cycle == null)
                    throw new ArgumentException("Cycle ID is invalid");

                // Get the phase and validate it belongs to the cycle
                var phase = await _treatmentPhaseRepository.GetTreatmentPhaseByIdAsync(phaseId);
                if (phase == null || phase.CycleId != cycleId)
                    throw new ArgumentException("Invalid phase ID or phase does not belong to the specified cycle");

                // Validate phase can be completed
                if (phase.Status != PhaseStatus.InProgress)
                    throw new InvalidOperationException($"Phase cannot be completed. Current status: {phase.Status}");

                // Validate end date
                if (phase.StartDate.HasValue && dto.EndDate < phase.StartDate.Value)
                    throw new ArgumentException("End date cannot be before start date");

                // Update phase
                phase.Status = PhaseStatus.Completed;
                phase.EndDate = dto.EndDate;
                
                if (!string.IsNullOrEmpty(dto.Results))
                {
                    phase.Notes = string.IsNullOrEmpty(phase.Notes) 
                        ? $"Results: {dto.Results}" 
                        : $"{phase.Notes}\nResults: {dto.Results}";
                }
                
                if (!string.IsNullOrEmpty(dto.NextPhaseInstructions))
                {
                    phase.Notes = string.IsNullOrEmpty(phase.Notes) 
                        ? $"Next Phase Instructions: {dto.NextPhaseInstructions}" 
                        : $"{phase.Notes}\nNext Phase Instructions: {dto.NextPhaseInstructions}";
                }
                
                if (!string.IsNullOrEmpty(dto.Notes))
                {
                    phase.Notes = string.IsNullOrEmpty(phase.Notes) 
                        ? dto.Notes 
                        : $"{phase.Notes}\n{dto.Notes}";
                }
                
                phase.UpdatedAt = DateTime.UtcNow;

                await _treatmentPhaseRepository.UpdateTreatmentPhaseAsync(phase);

                // Check if this was the last phase of the cycle -> neu cac phase deu completed -> cycle status up lai
                var allPhases = await _treatmentPhaseRepository.GetPhasesByCycleIdAsync(cycleId);
                var isLastPhase = allPhases.All(p => p.Status == PhaseStatus.Completed || p.Id == phaseId);
                
                if (isLastPhase && cycle.Status == CycleStatus.InProgress)
                {
                    cycle.Status = CycleStatus.Completed;
                    cycle.ActualEndDate = dto.EndDate;
                    cycle.UpdatedAt = DateTime.UtcNow;
                    await _treatmentCycleRepository.UpdateTreatmentCycleAsync(cycle);
                    _logger.LogInformation($"Cycle {cycleId} completed as all phases are finished");
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Phase {phaseId} completed for cycle {cycleId}");
                return _mapper.Map<PhaseResponseDto>(phase);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PhaseProgressDto> GetPhaseProgressAsync(int cycleId, int phaseId)
        {
            // Validate cycle exists
            var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
            if (cycle == null)
                throw new ArgumentException("Cycle ID is invalid");

            // Get the phase and validate it belongs to the cycle
            var phase = await _treatmentPhaseRepository.GetTreatmentPhaseByIdAsync(phaseId);
            if (phase == null || phase.CycleId != cycleId)
                throw new ArgumentException("Invalid phase ID or phase does not belong to the specified cycle");

            // Get appointments for this cycle (we'll consider them related to the current phase if in progress)
            var appointments = await _unitOfWork.Appointments.GetAppointmentsByCycleIdAsync(cycleId);
            var phaseAppointments = appointments.Where(a => 
                phase.StartDate.HasValue && a.ScheduledDateTime >= phase.StartDate.Value &&
                (!phase.EndDate.HasValue || a.ScheduledDateTime <= phase.EndDate.Value)
            ).ToList();

            // Get test results for this cycle
            var testResults = await _unitOfWork.TestResults.GetByCycleIdAsync(cycleId);
            var phaseTestResults = testResults.Where(t => 
                phase.StartDate.HasValue && t.CreatedAt >= phase.StartDate.Value &&
                (!phase.EndDate.HasValue || t.CreatedAt <= phase.EndDate.Value)
            ).ToList();

            // Calculate progress based on phase status and data
            double progressPercentage = phase.Status switch
            {
                PhaseStatus.Pending => 0,
                PhaseStatus.InProgress => CalculateInProgressPercentage(phase, phaseAppointments, phaseTestResults),
                PhaseStatus.Completed => 100,
                PhaseStatus.Cancelled => 0,
                PhaseStatus.OnHold => CalculateInProgressPercentage(phase, phaseAppointments, phaseTestResults),
                _ => 0
            };

            // Generate milestones based on treatment type and phase
            var (completed, pending, current) = GeneratePhaseMilestones(phase, phaseAppointments, phaseTestResults);

            var progressDto = new PhaseProgressDto
            {
                PhaseId = phase.Id,
                PhaseName = phase.PhaseName,
                Status = phase.Status,
                PhaseOrder = phase.PhaseOrder,
                ProgressPercentage = progressPercentage,
                CurrentMilestone = current,
                CompletedMilestones = completed,
                PendingMilestones = pending,
                StartDate = phase.StartDate,
                EstimatedEndDate = phase.StartDate?.AddDays(14), // Default 2 weeks per phase
                ActualEndDate = phase.EndDate,
                TotalAppointments = phaseAppointments.Count,
                CompletedAppointments = phaseAppointments.Count(a => a.Status == AppointmentStatus.Completed),
                TotalTestResults = phaseTestResults.Count,
                CompletedTestResults = phaseTestResults.Count(t => t.Status == TestResultStatus.Completed),
                Instructions = phase.Instructions,
                Notes = phase.Notes,
                LastUpdated = DateTime.UtcNow
            };

            return progressDto;
        }

        private double CalculateInProgressPercentage(TreatmentPhase phase, List<Appointment> appointments, List<TestResult> testResults)
        {
            if (appointments.Count == 0 && testResults.Count == 0)
                return 50; // Default 50% if no data available

            var completedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed);
            var completedTests = testResults.Count(t => t.Status == TestResultStatus.Completed);
            var totalItems = appointments.Count + testResults.Count;

            if (totalItems == 0)
                return 50;

            return Math.Round(((double)(completedAppointments + completedTests) / totalItems) * 100, 2);
        }

        private (List<string> completed, List<string> pending, string current) GeneratePhaseMilestones(
            TreatmentPhase phase, List<Appointment> appointments, List<TestResult> testResults)
        {
            var completed = new List<string>();
            var pending = new List<string>();
            var current = "Phase initiated";

            // Generate milestones based on phase name
            switch (phase.PhaseName.ToLower())
            {
                case "ovarian stimulation":
                    completed.Add("Baseline assessment completed");
                    if (appointments.Any(a => a.Status == AppointmentStatus.Completed))
                        completed.Add("Monitoring appointments started");
                    
                    current = "Ovarian stimulation in progress";
                    pending.Add("Complete stimulation protocol");
                    pending.Add("Final trigger injection");
                    break;

                case "egg retrieval":
                    completed.Add("Pre-procedure preparation");
                    current = "Egg retrieval procedure";
                    pending.Add("Post-procedure recovery");
                    pending.Add("Embryology report");
                    break;

                case "embryo transfer":
                    completed.Add("Embryo selection");
                    current = "Embryo transfer procedure";
                    pending.Add("Post-transfer care");
                    pending.Add("Pregnancy test");
                    break;

                default:
                    completed.Add("Phase started");
                    current = $"{phase.PhaseName} in progress";
                    pending.Add("Complete phase requirements");
                    break;
            }

            return (completed, pending, current);
        }

     

        #endregion

        #region Cycle Initialization Workflow Methods

        public async Task<CycleResponseDto> InitializeCycleAsync(int cycleId, InitializeCycleDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                if (cycle == null)
                    throw new ArgumentException("Cycle not found");

                if (cycle.Status != CycleStatus.Created)
                    throw new InvalidOperationException("Only cycles with 'Created' status can be initialized");

                // Update cycle with initialization data
                cycle.TreatmentPlan = dto.TreatmentPlan;
                cycle.SpecialInstructions = dto.SpecialInstructions;
                cycle.EstimatedCompletionDate = dto.EstimatedCompletionDate;
                cycle.Status = CycleStatus.Initialized;
                cycle.UpdatedAt = DateTime.UtcNow;

                await _treatmentCycleRepository.UpdateAsync(cycle);
                _logger.LogInformation("Cycle {CycleId} status updated to Initialized", cycleId);

                // Auto-generate default phases based on treatment package
                if (dto.AutoGeneratePhases && cycle.PackageId > 0)
                {
                    await GenerateDefaultPhasesForCycle(cycleId);
                }

                // Auto-schedule appointments for phases if requested
                if (dto.AutoScheduleAppointments)
                {
                    await AutoScheduleAppointmentsForCycle(cycleId, dto.StartDate);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Cycle {CycleId} initialized successfully", cycleId);
                
                var updatedCycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                return _mapper.Map<CycleResponseDto>(updatedCycle);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CycleResponseDto> StartTreatmentAsync(int cycleId, StartTreatmentDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                if (cycle == null)
                    throw new ArgumentException("Cycle not found");

                if (cycle.Status != CycleStatus.Initialized)
                    throw new InvalidOperationException("Only initialized cycles can start treatment");

                // Validate required tests are completed if specified
                if (dto.RequiredTestIds != null && dto.RequiredTestIds.Any())
                {
                    await ValidateRequiredTestsCompleted(cycleId, dto.RequiredTestIds);
                }

                // Update cycle to start treatment
                cycle.ActualStartDate = dto.ActualStartDate ?? DateTime.UtcNow;
                cycle.DoctorNotes = dto.DoctorNotes;
                cycle.Status = CycleStatus.InProgress; // Status = 3
                cycle.UpdatedAt = DateTime.UtcNow;

                await _treatmentCycleRepository.UpdateAsync(cycle);
                _logger.LogInformation("Cycle {CycleId} status updated to InProgress (3)", cycleId);

                // Start the first phase if it exists
                var firstPhase = await _treatmentPhaseRepository.GetFirstPhaseForCycleAsync(cycleId);
                if (firstPhase != null && firstPhase.Status == PhaseStatus.Pending)
                {
                    firstPhase.Status = PhaseStatus.InProgress;
                    firstPhase.ActualStartDate = cycle.ActualStartDate;
                    firstPhase.UpdatedAt = DateTime.UtcNow;
                    await _treatmentPhaseRepository.UpdateAsync(firstPhase);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Treatment started for cycle {CycleId}", cycleId);
                
                var updatedCycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
                return _mapper.Map<CycleResponseDto>(updatedCycle);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CycleTimelineDto> GetCycleTimelineAsync(int cycleId)
        {
            var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
            if (cycle == null)
                throw new ArgumentException("Cycle not found");

            var phases = await _treatmentPhaseRepository.GetPhasesByCycleIdAsync(cycleId);
            var timelineEvents = new List<TimelineEventDto>();
            var eventIdCounter = 1; // Generate unique IDs for timeline events

            // Add cycle creation event
            timelineEvents.Add(new TimelineEventDto
            {
                Id = eventIdCounter++,
                EventType = "Cycle Created",
                Title = "Cycle Created",
                EventDate = cycle.CreatedAt,
                Description = "Treatment cycle was created",
                Status = "Completed",
                IsCompleted = true
            });

            // Add cycle initialization event if applicable
            if (cycle.Status != CycleStatus.Created)
            {
                timelineEvents.Add(new TimelineEventDto
                {
                    Id = eventIdCounter++,
                    EventType = "Cycle Initialized",
                    Title = "Treatment Plan Finalized",
                    EventDate = cycle.UpdatedAt ?? cycle.CreatedAt,
                    Description = "Treatment plan was finalized and cycle initialized",
                    Status = "Completed",
                    IsCompleted = true
                });
            }

            // Add treatment start event if applicable
            if (cycle.ActualStartDate.HasValue)
            {
                timelineEvents.Add(new TimelineEventDto
                {
                    Id = eventIdCounter++,
                    EventType = "Treatment Started",
                    Title = "Active Treatment Began",
                    EventDate = cycle.ActualStartDate.Value,
                    Description = "Active treatment phase has begun",
                    Status = "Completed",
                    IsCompleted = true
                });
            }

            // Add phase events
            foreach (var phase in phases.OrderBy(p => p.PhaseOrder))
            {
                var phaseStatus = phase.Status switch
                {
                    PhaseStatus.Pending => "Pending",
                    PhaseStatus.InProgress => "In Progress",
                    PhaseStatus.Completed => "Completed",
                    PhaseStatus.Cancelled => "Cancelled",
                    _ => "Unknown"
                };

                var eventDate = phase.ActualStartDate ?? phase.ScheduledStartDate ?? phase.CreatedAt;
                var isCompleted = phase.Status == PhaseStatus.Completed;
                
                timelineEvents.Add(new TimelineEventDto
                {
                    Id = eventIdCounter++,
                    EventType = $"Phase: {phase.PhaseName}",
                    Title = phase.PhaseName,
                    EventDate = eventDate,
                    Description = phase.Instructions ?? $"{phase.PhaseName} phase",
                    Status = phaseStatus,
                    IsCompleted = isCompleted,
                    PhaseId = phase.Id
                });
            }

            // Add completion event if cycle is completed
            if (cycle.Status == CycleStatus.Completed)
            {
                timelineEvents.Add(new TimelineEventDto
                {
                    Id = eventIdCounter++,
                    EventType = "Treatment Completed",
                    Title = "Treatment Cycle Completed",
                    EventDate = cycle.UpdatedAt ?? DateTime.UtcNow,
                    Description = "Treatment cycle has been completed",
                    Status = "Completed",
                    IsCompleted = true
                });
            }

            // Calculate completion percentage
            var totalEvents = timelineEvents.Count;
            var completedEvents = timelineEvents.Count(e => e.IsCompleted);
            var completionPercentage = totalEvents > 0 ? (int)((double)completedEvents / totalEvents * 100) : 0;

            return new CycleTimelineDto
            {
                CycleId = cycleId,
                CycleName = $"Treatment Cycle #{cycle.Id}",
                OverallStatus = cycle.Status.ToString(),
                CreatedDate = cycle.CreatedAt,
                StartDate = cycle.ActualStartDate,
                EstimatedCompletionDate = cycle.EstimatedCompletionDate,
                CompletionPercentage = completionPercentage,
                TimelineEvents = timelineEvents.OrderBy(e => e.EventDate).ToList()
            };
        }

        private async Task GenerateDefaultPhasesForCycle(int cycleId)
        {
            var defaultPhases = GetDefaultPhases(cycleId);
            
            foreach (var phase in defaultPhases)
            {
                await _treatmentPhaseRepository.AddTreatmentPhaseAsync(phase);
            }
        }

        private List<TreatmentPhase> GetDefaultPhases(int cycleId)
        {
            return


                 GeneratePhases(cycleId);
              
        }

        

        private List<TreatmentPhase> GeneratePhases(int cycleId)
        {
            return new List<TreatmentPhase>
            {
                new TreatmentPhase
                {
                    CycleId = cycleId,
                    PhaseName = "Pre-treatment Assessment",
                    PhaseOrder = 1,
                    Status = PhaseStatus.Pending,
                    Instructions = "Comprehensive evaluation and baseline tests",
                    Cost = 800,
                    CreatedAt = DateTime.UtcNow
                },
                new TreatmentPhase
                {
                    CycleId = cycleId,
                    PhaseName = "Ovarian Stimulation",
                    PhaseOrder = 2,
                    Status = PhaseStatus.Pending,
                    Instructions = "Controlled ovarian hyperstimulation with monitoring",
                    Cost = 2000,
                    CreatedAt = DateTime.UtcNow
                },
                new TreatmentPhase
                {
                    CycleId = cycleId,
                    PhaseName = "Egg Retrieval",
                    PhaseOrder = 3,
                    Status = PhaseStatus.Pending,
                    Instructions = "Oocyte retrieval procedure under sedation",
                    Cost = 3000,
                    CreatedAt = DateTime.UtcNow
                },
                new TreatmentPhase
                {
                    CycleId = cycleId,
                    PhaseName = "Fertilization & Culture",
                    PhaseOrder = 4,
                    Status = PhaseStatus.Pending,
                    Instructions = "In vitro fertilization and embryo culture",
                    Cost = 2500,
                    CreatedAt = DateTime.UtcNow
                },
                new TreatmentPhase
                {
                    CycleId = cycleId,
                    PhaseName = "Embryo Transfer",
                    PhaseOrder = 5,
                    Status = PhaseStatus.Pending,
                    Instructions = "Transfer of selected embryos to uterus",
                    Cost = 1500,
                    CreatedAt = DateTime.UtcNow
                },
                new TreatmentPhase
                {
                    CycleId = cycleId,
                    PhaseName = "Post-Transfer Monitoring",
                    PhaseOrder = 6,
                    Status = PhaseStatus.Pending,
                    Instructions = "Luteal support and pregnancy monitoring",
                    Cost = 400,
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        #endregion

        private async Task AutoScheduleAppointmentsForCycle(int cycleId, DateTime startDate)
        {
            var phases = await _treatmentPhaseRepository.GetPhasesByCycleIdAsync(cycleId);
            var cycle = await _treatmentCycleRepository.GetByIdAsync(cycleId);
            
            if (cycle == null || !phases.Any() || cycle.DoctorId == 0)
            {
                _logger.LogWarning("Cannot auto-schedule appointments: cycle {CycleId} not found, has no phases, or no doctor assigned", cycleId);
                return;
            }

            var currentDate = startDate;
            var successfullyScheduled = 0;
            
            foreach (var phase in phases.OrderBy(p => p.PhaseOrder))
            {
                try
                {
                    // Calculate preferred appointment date for this phase
                    var preferredDate = CalculateAppointmentDate(phase, currentDate);
                    
                    // Try to find available appointment slot within a reasonable window (±3 days)
                    var appointmentSlot = await FindAvailableAppointmentSlot(cycle.DoctorId, preferredDate, 3);
                    
                    if (appointmentSlot != null)
                    {
                        // Create the appointment
                        var appointment = new Appointment
                        {
                            CycleId = cycleId,
                            DoctorId = cycle.DoctorId,
                            CustomerId = cycle.CustomerId,
                            DoctorScheduleId = appointmentSlot.ScheduleId,
                            AppointmentType = GetAppointmentTypeForPhase(phase.PhaseName),
                            ScheduledDateTime = appointmentSlot.DateTime,
                            Notes = $"Auto-scheduled for {phase.PhaseName}",
                            Status = AppointmentStatus.Scheduled
                        };

                        await _appointmentRepository.CreateAsync(appointment);
                        
                        // Update phase with scheduled start date
                        phase.ScheduledStartDate = appointmentSlot.DateTime;
                        await _treatmentPhaseRepository.UpdateAsync(phase);
                        
                        successfullyScheduled++;
                        currentDate = appointmentSlot.DateTime.AddDays(GetPhaseDurationDays(phase.PhaseName));
                        
                        _logger.LogInformation(
                            "Successfully scheduled appointment for Phase {PhaseName} on {Date} for Cycle {CycleId}",
                            phase.PhaseName, appointmentSlot.DateTime, cycleId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Could not find available slot for Phase {PhaseName} around {PreferredDate} for Cycle {CycleId}",
                            phase.PhaseName, preferredDate, cycleId);
                        
                        // Still update the phase with preferred date (even if not confirmed)
                        phase.ScheduledStartDate = preferredDate;
                        await _treatmentPhaseRepository.UpdateAsync(phase);
                        
                        // Move to next phase date anyway
                        currentDate = preferredDate.AddDays(GetPhaseDurationDays(phase.PhaseName));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error scheduling appointment for Phase {PhaseName} in Cycle {CycleId}", 
                        phase.PhaseName, cycleId);
                    
                    // Continue with next phase
                    currentDate = currentDate.AddDays(GetPhaseDurationDays(phase.PhaseName));
                }
            }
            
            _logger.LogInformation(
                "Auto-scheduling completed for Cycle {CycleId}: {Successful}/{Total} appointments scheduled",
                cycleId, successfullyScheduled, phases.Count());
        }

        private DateTime CalculateAppointmentDate(TreatmentPhase phase, DateTime baseDate)
        {
            // Calculate appointment date based on phase type
            return phase.PhaseName.ToLower() switch
            {
                "pre-treatment assessment" => baseDate.AddDays(1), // Next day
                "initial consultation" => baseDate.AddDays(1),
                "ovulation monitoring" => baseDate.AddDays(3), // Give time for preparation
                "ovarian stimulation" => baseDate.AddDays(7), // Week later
                "egg retrieval" => baseDate.AddDays(14), // 2 weeks later
                "insemination" => baseDate.AddDays(14), // For IUI
                "fertilization & culture" => baseDate.AddDays(15), // Day after retrieval
                "embryo transfer" => baseDate.AddDays(19), // 5 days after retrieval
                "post-transfer monitoring" => baseDate.AddDays(21), // 2 days after transfer
                "post-iui monitoring" => baseDate.AddDays(16), // 2 days after IUI
                _ => baseDate.AddDays(1)
            };
        }

        private int GetPhaseDurationDays(string phaseName)
        {
            // Return typical duration for each phase type
            return phaseName.ToLower() switch
            {
                "pre-treatment assessment" => 3,
                "initial consultation" => 2,
                "ovulation monitoring" => 7,
                "ovarian stimulation" => 10,
                "egg retrieval" => 1,
                "insemination" => 1,
                "fertilization & culture" => 5,
                "embryo transfer" => 1,
                "post-transfer monitoring" => 14,
                "post-iui monitoring" => 14,
                _ => 7 // Default 1 week
            };
        }

        private async Task ValidateRequiredTestsCompleted(int cycleId, List<int> requiredTestIds)
        {
            // This would require a test result repository
            // For now, we'll implement basic validation logic
            
            var incompletedTests = new List<int>();
            
            foreach (var testId in requiredTestIds)
            {
                // Check if test result exists and is completed
                // This is a placeholder - you would need to implement actual test result checking
                var isTestCompleted = await CheckTestResultCompleted(cycleId, testId);
                
                if (!isTestCompleted)
                {
                    incompletedTests.Add(testId);
                }
            }
            
            if (incompletedTests.Any())
            {
                var testList = string.Join(", ", incompletedTests);
                throw new InvalidOperationException(
                    $"Cannot start treatment. The following required tests are not completed: {testList}. " +
                    "Please ensure all required tests are completed before starting treatment.");
            }
        }

        private async Task<bool> CheckTestResultCompleted(int cycleId, int testId)
        {
            try
            {
                // Check if the test result exists and is completed for this cycle
                return await _testResultRepository.IsTestCompletedAsync(cycleId, testId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking test result completion for test {TestId} in cycle {CycleId}", testId, cycleId);
                // If we can't verify the test, assume it's not completed for safety
                return false;
            }
        }

        /// <summary>
        /// Find available appointment slot for doctor within a date window
        /// </summary>
        private async Task<AppointmentSlot?> FindAvailableAppointmentSlot(int doctorId, DateTime preferredDate, int windowDays)
        {
            for (int dayOffset = 0; dayOffset <= windowDays; dayOffset++)
            {
                // Try preferred date, then ±1, ±2, ±3 days
                var dates = new List<DateTime>();
                
                if (dayOffset == 0)
                {
                    dates.Add(preferredDate);
                }
                else
                {
                    dates.Add(preferredDate.AddDays(dayOffset));   // +1, +2, +3
                    dates.Add(preferredDate.AddDays(-dayOffset));  // -1, -2, -3
                }

                foreach (var date in dates)
                {
                    // Skip weekends if necessary
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    // Skip past dates
                    if (date.Date < DateTime.Now.Date)
                        continue;

                    // Get doctor's schedule for this date
                    var schedules = await _doctorScheduleRepository.GetSchedulesByDoctorAndDateAsync(doctorId, date);
                    
                    foreach (var schedule in schedules.Where(s => s.IsActive))
                    {
                        // Check if this schedule slot is available (no conflicting appointment)
                        var conflict = await _appointmentRepository.GetByDoctorAndScheduleAsync(
                            doctorId, date, schedule.Id);

                        if (conflict == null)
                        {
                            // Found available slot
                            return new AppointmentSlot
                            {
                                DateTime = new DateTime(date.Year, date.Month, date.Day, 
                                                      schedule.StartTime.Hours, schedule.StartTime.Minutes, 0),
                                ScheduleId = schedule.Id
                            };
                        }
                    }
                }
            }

            return null; // No available slot found
        }

        /// <summary>
        /// Get appropriate appointment type based on phase name
        /// </summary>
        private AppointmentType GetAppointmentTypeForPhase(string phaseName)
        {
            return phaseName.ToLower() switch
            {
                "pre-treatment assessment" => AppointmentType.Consultation,
                "initial consultation" => AppointmentType.Consultation,
                "ovulation monitoring" => AppointmentType.PeriodicCheck,
                "ovarian stimulation" => AppointmentType.Injection,
                "egg retrieval" => AppointmentType.Consultation, // Use closest available
                "insemination" => AppointmentType.Consultation,
                "embryo transfer" => AppointmentType.Consultation,
                "pregnancy test" => AppointmentType.PeriodicCheck,
                "follow-up" => AppointmentType.FollowUp,
                _ => AppointmentType.Consultation
            };
        }

        /// <summary>
        /// Helper class to represent an available appointment slot
        /// </summary>
        private class AppointmentSlot
        {
            public DateTime DateTime { get; set; }
            public int ScheduleId { get; set; }
        }
    }
}
