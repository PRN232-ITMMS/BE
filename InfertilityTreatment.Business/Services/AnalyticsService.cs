using InfertilityTreatment.Business.Exceptions;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Analytics;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using InfertilityTreatment.Entity.Entities;
using System.Globalization;

namespace InfertilityTreatment.Business.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnalyticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(UserRole role, int? userId, DateRangeDto dateRange)
        {
            var startDate = dateRange.StartDate;
            var endDate = dateRange.EndDate;

            // For non-admin roles, we need to validate the user exists
            User user = null;
            if (userId.HasValue)
            {
                user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId.Value);
                if (user == null)
                {
                    throw new NotFoundException("User not found");
                }
            }

            // Get all cycles and filter by date
            var allCycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var cycles = allCycles.Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate);

            // Filter by user role
            if (role == UserRole.Doctor && userId.HasValue)
            {
                var doctorId = user?.Doctor?.Id ?? userId.Value;
                cycles = cycles.Where(c => c.DoctorId == doctorId);
            }
            else if (role == UserRole.Customer && userId.HasValue)
            {
                var customerId = user?.Customer?.Id ?? userId.Value;
                cycles = cycles.Where(c => c.CustomerId == customerId);
            }
            // For Admin role, we don't filter by userId - show all data for the specified role

            var cyclesList = cycles.ToList();

            // Get all appointments and filter by date
            var allAppointments = await _unitOfWork.Appointments.GetAllAsync();
            var appointments = allAppointments.Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate);

            if (role == UserRole.Doctor && userId.HasValue)
            {
                var doctorId = user?.Doctor?.Id ?? userId.Value;
                appointments = appointments.Where(a => a.DoctorId == doctorId);
            }
            else if (role == UserRole.Customer && userId.HasValue)
            {
                var customerCycleIds = cyclesList.Select(c => c.Id).ToList();
                appointments = appointments.Where(a => a.CycleId.HasValue && customerCycleIds.Contains(a.CycleId.Value));

            }
            // For Admin role, we don't filter appointments - show all data for the specified role

            var appointmentsList = appointments.ToList();

            // Calculate stats
            var totalPatients = role == UserRole.Doctor && userId.HasValue
                ? cyclesList.Select(c => c.CustomerId).Distinct().Count()
                : (role == UserRole.Customer && userId.HasValue ? 1 : cyclesList.Select(c => c.CustomerId).Distinct().Count());

            var activeTreatments = cyclesList.Count(c => c.Status == CycleStatus.InProgress);
            var completedTreatments = cyclesList.Count(c => c.Status == CycleStatus.Completed);
            var cancelledTreatments = cyclesList.Count(c => c.Status == CycleStatus.Cancelled);
            var successRate = (completedTreatments + cancelledTreatments) > 0 ?
                (double)completedTreatments / (completedTreatments + cancelledTreatments) * 100 : 0;

            // Calculate revenue from packages
            var packageIds = cyclesList.Select(c => c.PackageId).Distinct().ToList();
            var allPackages = await _unitOfWork.TreatmentPackages.GetAllAsync();
            var packages = allPackages.Where(p => packageIds.Contains(p.Id)).ToList();

            var totalRevenue = cyclesList.Sum(c => packages.FirstOrDefault(p => p.Id == c.PackageId)?.Price ?? 0);

            return new DashboardStatsDto
            {
                TotalPatients = totalPatients,
                TotalAppointments = appointmentsList.Count,
                TotalTreatmentCycles = cyclesList.Count,
                TotalRevenue = totalRevenue,
                ActiveTreatments = activeTreatments,
                CompletedTreatments = completedTreatments,
                SuccessRate = successRate,
                LastUpdated = DateTime.Now
            };
        }

        public async Task<PaginatedResultDto<TreatmentSuccessRateDto>> GetTreatmentSuccessRatesAsync(SuccessRateFilterDto filter, PaginationQueryDTO pagination)
        {
            var treatmentServices = (await _unitOfWork.TreatmentServices.GetAllAsync()).ToList();
            var packages = (await _unitOfWork.TreatmentPackages.GetAllAsync()).ToList();
            var cycles = (await _unitOfWork.TreatmentCycles.GetAllAsync()).ToList();

            // Apply filters
            if (filter.ServiceId.HasValue)
            {
                treatmentServices = treatmentServices.Where(s => s.Id == filter.ServiceId.Value).ToList();
                if (!treatmentServices.Any())
                {
                    throw new KeyNotFoundException("Không tìm thấy dịch vụ phù hợp với ServiceId được cung cấp.");
                }
            }

            if (filter.DoctorId.HasValue)
            {
                cycles = cycles.Where(c => c.DoctorId == filter.DoctorId.Value).ToList();
                if (!cycles.Any())
                {
                    throw new KeyNotFoundException("Không tìm thấy dịch vụ phù hợp với DoctorId được cung cấp.");
                }
            }

            var result = new List<TreatmentSuccessRateDto>();

            foreach (var service in treatmentServices)
            {
                var servicePackages = packages.Where(p => p.ServiceId == service.Id).Select(p => p.Id).ToList();
                var serviceCycles = cycles.Where(c => servicePackages.Contains(c.PackageId)).ToList();

                if (serviceCycles.Any())
                {
                    var totalCycles = serviceCycles.Count;
                    var successfulCycles = serviceCycles.Count(c => c.Status == CycleStatus.Completed);
                    var successRate = totalCycles > 0 ? (double)successfulCycles / totalCycles * 100 : 0;

                    result.Add(new TreatmentSuccessRateDto
                    {
                        TreatmentType = service.Name,
                        TotalCycles = totalCycles,
                        SuccessfulCycles = successfulCycles,
                        SuccessRate = successRate,
                        PeriodStart = serviceCycles.Min(c => c.CreatedAt),
                        PeriodEnd = serviceCycles.Max(c => c.CreatedAt)
                    });
                }
            }

            // Sort results
            var orderedResult = result.OrderByDescending(r => r.SuccessRate).ToList();

            // Apply pagination
            var totalCount = orderedResult.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);
            var skip = (pagination.PageNumber - 1) * pagination.PageSize;
            var paginatedItems = orderedResult.Skip(skip).Take(pagination.PageSize).ToList();

            return new PaginatedResultDto<TreatmentSuccessRateDto>(
                     paginatedItems,
                    totalCount,
                    pagination.PageNumber,
                    pagination.PageSize
                );
        }

        public async Task<RevenueReportDto> GetRevenueReportAsync(RevenueFilterDto filter)
        {
            var allCycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var cycles = allCycles.Where(c => c.CreatedAt >= filter.StartDate && c.CreatedAt <= filter.EndDate);

            if (filter.DoctorId.HasValue)
            {
                cycles = cycles.Where(c => c.DoctorId == filter.DoctorId.Value);
            }

            var cyclesList = cycles.ToList();

            // Get packages for revenue calculation
            var packageIds = cyclesList.Select(c => c.PackageId).Distinct().ToList();
            var allPackages = await _unitOfWork.TreatmentPackages.GetAllAsync();
            var packages = allPackages.Where(p => packageIds.Contains(p.Id)).ToList();

            // Get treatment services for package details
            var serviceIds = packages.Select(p => p.ServiceId).Distinct().ToList();
            var allServices = await _unitOfWork.TreatmentServices.GetAllAsync();
            var services = allServices.Where(s => serviceIds.Contains(s.Id)).ToList();

            var totalRevenue = cyclesList.Sum(c => packages.FirstOrDefault(p => p.Id == c.PackageId)?.Price ?? 0);

            // Monthly breakdown
            var monthlyData = cyclesList
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(c => packages.FirstOrDefault(p => p.Id == c.PackageId)?.Price ?? 0),
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            // Service breakdown
            var serviceData = new List<ServiceRevenueDto>();
            foreach (var service in services)
            {
                var servicePackageIds = packages.Where(p => p.ServiceId == service.Id).Select(p => p.Id).ToList();
                var serviceCycles = cyclesList.Where(c => servicePackageIds.Contains(c.PackageId)).ToList();

                if (serviceCycles.Any())
                {
                    var serviceRevenue = serviceCycles.Sum(c => packages.FirstOrDefault(p => p.Id == c.PackageId)?.Price ?? 0);
                    serviceData.Add(new ServiceRevenueDto
                    {
                        ServiceName = service.Name,
                        Revenue = serviceRevenue,
                        BookingCount = serviceCycles.Count,
                        Percentage = totalRevenue > 0 ? (double)(serviceRevenue / totalRevenue) * 100 : 0
                    });
                }
            }

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = monthlyData.FirstOrDefault(m => m.Month == currentMonth && m.Year == currentYear)?.Revenue ?? 0;
            var yearlyRevenue = monthlyData.Where(m => m.Year == currentYear).Sum(m => m.Revenue);

            return new RevenueReportDto
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                YearlyRevenue = yearlyRevenue,
                MonthlyBreakdown = monthlyData,
                ServiceBreakdown = serviceData.OrderByDescending(s => s.Revenue).ToList(),
                ReportDate = DateTime.Now
            };
        }

        public async Task<DoctorPerformanceDto> GetDoctorPerformanceAsync(int doctorId)
        {
            var doctor = await _unitOfWork.Doctors.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found");
            }

            // Get user details
            var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);

            var allCycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var cycles = allCycles.Where(c => c.DoctorId == doctorId).ToList();

            var allAppointments = await _unitOfWork.Appointments.GetAllAsync();
            var appointments = allAppointments.Where(a => a.DoctorId == doctorId).ToList();

            var allReviews = await _unitOfWork.Reviews.GetAllAsync();
            var reviews = allReviews.Where(r => r.DoctorId == doctorId).ToList();

            // Get packages for revenue calculation
            var packageIds = cycles.Select(c => c.PackageId).Distinct().ToList();
            var allPackages = await _unitOfWork.TreatmentPackages.GetAllAsync();
            var packages = allPackages.Where(p => packageIds.Contains(p.Id)).ToList();

            var totalPatients = cycles.Select(c => c.CustomerId).Distinct().Count();
            var completedTreatments = cycles.Count(c => c.Status == CycleStatus.Completed);
            var totalTreatments = cycles.Count(c => c.Status == CycleStatus.Completed || c.Status == CycleStatus.Cancelled);
            var successRate = totalTreatments > 0 ? (double)completedTreatments / totalTreatments * 100 : 0;

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var totalRevenue = cycles.Sum(c => packages.FirstOrDefault(p => p.Id == c.PackageId)?.Price ?? 0);

            // Monthly performance
            var monthlyPerformance = cycles
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new MonthlyPerformanceDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Appointments = appointments.Count(a => a.CreatedAt.Year == g.Key.Year && a.CreatedAt.Month == g.Key.Month),
                    Revenue = g.Sum(c => packages.FirstOrDefault(p => p.Id == c.PackageId)?.Price ?? 0),
                    Rating = reviews.Where(r => r.CreatedAt.Year == g.Key.Year && r.CreatedAt.Month == g.Key.Month)
                                   .Any() ? reviews.Where(r => r.CreatedAt.Year == g.Key.Year && r.CreatedAt.Month == g.Key.Month)
                                                  .Average(r => r.Rating) : 0
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            return new DoctorPerformanceDto
            {
                DoctorId = doctorId,
                DoctorName = user?.FullName ?? "Unknown",
                Specialization = doctor.Specialization ?? "General",
                TotalPatients = totalPatients,
                TotalAppointments = appointments.Count,
                CompletedTreatments = completedTreatments,
                SuccessRate = successRate,
                AverageRating = averageRating,
                TotalReviews = reviews.Count,
                TotalRevenue = totalRevenue,
                MonthlyPerformance = monthlyPerformance,
                LastUpdated = DateTime.Now
            };
        }

        public async Task<PatientDemographicsDto> GetPatientDemographicsAsync(DateRangeDto dateRange)
        {
            var customers = await _unitOfWork.Customers.GetAllWithUserAsync();
            var cycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var packages = await _unitOfWork.TreatmentPackages.GetAllAsync();
            var services = await _unitOfWork.TreatmentServices.GetAllAsync();

            // Filter cycles by date range
            var filteredCycles = cycles
                .Where(c => c.CreatedAt >= dateRange.StartDate && c.CreatedAt <= dateRange.EndDate)
                .ToList();

            var customerIds = filteredCycles
                .Select(c => c.CustomerId)
                .Distinct()
                .ToList();

            var filteredCustomers = customers
                .Where(c => customerIds.Contains(c.Id))
                .ToList();

            var totalPatients = filteredCustomers.Count();
            var totalCycles = filteredCycles.Count;

            // Treatment types: group by service
            var treatmentTypes = new List<TreatmentTypeDto>();
            foreach (var service in services)
            {
                var servicePackages = packages
                    .Where(p => p.ServiceId == service.Id)
                    .Select(p => p.Id)
                    .ToList();

                var count = filteredCycles.Count(c => servicePackages.Contains(c.PackageId));

                if (count > 0)
                {
                    treatmentTypes.Add(new TreatmentTypeDto
                    {
                        TreatmentType = service.Name,
                        Count = count,
                        Percentage = totalCycles > 0 ? Math.Round((double)count / totalCycles * 100, 2) : 0
                    });
                }
            }

            // Gender distribution
            int amountMale = filteredCustomers.Count(c => c.User != null && c.User.Gender == Gender.Male);
            int amountFemale = filteredCustomers.Count(c => c.User != null && c.User.Gender == Gender.Female);
            int amountOther = filteredCustomers.Count(c => c.User != null && c.User.Gender == Gender.Other);

            return new PatientDemographicsDto
            {
                TotalPatients = totalPatients,
                TreatmentTypes = treatmentTypes,
                GenderDistribution = new GenderDistributionDto
                {
                    Male = amountMale,
                    Female = amountFemale,
                    Other = amountOther
                }
            };
        }

        public async Task<PaginatedResultDto<OutcomeAnalysisResultDto>> GetTreatmentOutcomesAsync(OutcomeAnalysisDto filters)
        {
            // Get all cycles within the date range
            var allCycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var cycles = allCycles.Where(c => c.CreatedAt >= filters.StartDate && c.CreatedAt <= filters.EndDate);

            // Filter by treatment type if specified
            if (!string.IsNullOrEmpty(filters.TreatmentType))
            {
                var packages = await _unitOfWork.TreatmentPackages.GetAllAsync();
                var services = await _unitOfWork.TreatmentServices.GetAllAsync();

                var targetService = services.FirstOrDefault(s => s.Name.Contains(filters.TreatmentType, StringComparison.OrdinalIgnoreCase));
                if (targetService != null)
                {
                    var servicePackageIds = packages.Where(p => p.ServiceId == targetService.Id).Select(p => p.Id).ToList();
                    cycles = cycles.Where(c => servicePackageIds.Contains(c.PackageId));
                }
            }

            // Filter by doctor if specified
            if (filters.DoctorId.HasValue)
            {
                cycles = cycles.Where(c => c.DoctorId == filters.DoctorId.Value);
            }

            var cyclesList = cycles.ToList();

            var totalCount = cyclesList.Count;
            var pageNumber = filters?.PageNumber ?? 1;
            var pageSize = filters?.PageSize ?? 100;
            var pagedCycles = cyclesList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            if (!pagedCycles.Any())
            {
                return new PaginatedResultDto<OutcomeAnalysisResultDto>(new List<OutcomeAnalysisResultDto>(), totalCount, pageNumber, pageSize);

            }

            // Calculate overall success rate
            var completedCycles = pagedCycles.Where(c => c.Status == CycleStatus.Completed).ToList();
            var cancelledCycles = pagedCycles.Where(c => c.Status == CycleStatus.Cancelled).ToList();
            var totalFinishedCycles = completedCycles.Count + cancelledCycles.Count;
            var successRate = totalFinishedCycles > 0 ? (double)completedCycles.Count / totalFinishedCycles * 100 : 0;

            // Calculate success rate by group
            var successRateByGroup = new Dictionary<string, double>();

            if (filters.GroupBy?.ToLower() == "doctor")
            {
                var doctorsResult = await _unitOfWork.Doctors.GetDoctorsAsync(new InfertilityTreatment.Entity.DTOs.Doctors.DoctorFilterDto());
                var doctors = doctorsResult.Doctors;
                var users = await _unitOfWork.Users.GetAllAsync();

                foreach (var doctor in doctors)
                {
                    var doctorCycles = pagedCycles.Where(c => c.DoctorId == doctor.Id).ToList();
                    var doctorCompleted = doctorCycles.Count(c => c.Status == CycleStatus.Completed);
                    var doctorCancelled = doctorCycles.Count(c => c.Status == CycleStatus.Cancelled);
                    var doctorTotal = doctorCompleted + doctorCancelled;

                    if (doctorTotal > 0)
                    {
                        var doctorSuccessRate = (double)doctorCompleted / doctorTotal * 100;
                        var doctorName = users.FirstOrDefault(u => u.Id == doctor.UserId)?.FullName ?? $"Doctor {doctor.Id}";
                        successRateByGroup[doctorName] = doctorSuccessRate;
                    }
                }
            }
            else if (filters.GroupBy?.ToLower() == "age")
            {
                var customers = await _unitOfWork.Customers.GetAllWithUserAsync();
                var customerIds = pagedCycles.Select(c => c.CustomerId).Distinct().ToList();
                var relevantCustomers = customers.Where(c => customerIds.Contains(c.Id)).ToList();

                var ageGroups = new Dictionary<string, List<int>>
                {
                    ["18-25"] = new List<int>(),
                    ["26-35"] = new List<int>(),
                    ["36-45"] = new List<int>(),
                    ["46+"] = new List<int>()
                };

                // Since User doesn't have DateOfBirth, we'll use a simplified age distribution
                var customerList = relevantCustomers.ToList();
                var groupSize = customerList.Count / 4;
                var remainder = customerList.Count % 4;

                for (int i = 0; i < customerList.Count; i++)
                {
                    var groupIndex = i / (groupSize + (i < remainder * (groupSize + 1) ? 1 : 0));
                    var groupKey = groupIndex switch
                    {
                        0 => "18-25",
                        1 => "26-35",
                        2 => "36-45",
                        _ => "46+"
                    };
                    ageGroups[groupKey].Add(customerList[i].Id);
                }

                foreach (var ageGroup in ageGroups)
                {
                    var groupCycles = pagedCycles.Where(c => ageGroup.Value.Contains(c.CustomerId)).ToList();
                    var groupCompleted = groupCycles.Count(c => c.Status == CycleStatus.Completed);
                    var groupCancelled = groupCycles.Count(c => c.Status == CycleStatus.Cancelled);
                    var groupTotal = groupCompleted + groupCancelled;

                    if (groupTotal > 0)
                    {
                        var groupSuccessRate = (double)groupCompleted / groupTotal * 100;
                        successRateByGroup[ageGroup.Key] = groupSuccessRate;
                    }
                }
            }

            // Calculate time to success
            var timeToSuccessList = new List<TimeSpan>();
            foreach (var cycle in completedCycles)
            {
                if (cycle.ActualEndDate.HasValue)
                {
                    var timeToSuccess = cycle.ActualEndDate.Value - cycle.CreatedAt;
                    timeToSuccessList.Add(timeToSuccess);
                }
            }

            // Doctor comparisons
            var doctorComparisons = new List<DoctorComparisonDto>();
            var allDoctorsResult = await _unitOfWork.Doctors.GetDoctorsAsync(new InfertilityTreatment.Entity.DTOs.Doctors.DoctorFilterDto());
            var allDoctors = allDoctorsResult.Doctors;
            var allUsers = await _unitOfWork.Users.GetAllAsync();

            foreach (var doctor in allDoctors)
            {
                var doctorCycles = pagedCycles.Where(c => c.DoctorId == doctor.Id).ToList();
                var doctorCompleted = doctorCycles.Count(c => c.Status == CycleStatus.Completed);
                var doctorCancelled = doctorCycles.Count(c => c.Status == CycleStatus.Cancelled);
                var doctorTotal = doctorCompleted + doctorCancelled;

                if (doctorTotal > 0)
                {
                    var doctorSuccessRate = (double)doctorCompleted / doctorTotal * 100;
                    var doctorName = allUsers.FirstOrDefault(u => u.Id == doctor.UserId)?.FullName ?? $"Doctor {doctor.Id}";

                    doctorComparisons.Add(new DoctorComparisonDto
                    {
                        DoctorId = doctor.Id,
                        DoctorName = doctorName,
                        SuccessRate = doctorSuccessRate
                    });
                }
            }

            // Trend data (monthly success rates)
            var trendData = new List<TrendPointDto>();
            var monthlyGroups = pagedCycles
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            foreach (var group in monthlyGroups)
            {
                var monthCompleted = group.Count(c => c.Status == CycleStatus.Completed);
                var monthCancelled = group.Count(c => c.Status == CycleStatus.Cancelled);
                var monthTotal = monthCompleted + monthCancelled;

                if (monthTotal > 0)
                {
                    var monthSuccessRate = (double)monthCompleted / monthTotal * 100;
                    trendData.Add(new TrendPointDto
                    {
                        Date = new DateTime(group.Key.Year, group.Key.Month, 1),
                        Value = monthSuccessRate
                    });
                }
            }

            var result = new OutcomeAnalysisResultDto
            {
                SuccessRate = successRate,
                SuccessRateByGroup = successRateByGroup,
                TimeToSuccessList = timeToSuccessList,
                DoctorComparisons = doctorComparisons.OrderByDescending(d => d.SuccessRate).ToList(),
                TrendData = trendData
            };

            return new PaginatedResultDto<OutcomeAnalysisResultDto>(new List<OutcomeAnalysisResultDto> { result }, totalCount, pageNumber, pageSize);

        }

        public async Task<EfficiencyMetrics> GetEfficiencyMetricsAsync(EfficiencyQueryDto query)
        {
            // Get all appointments within the date range
            var allAppointments = await _unitOfWork.Appointments.GetAllAsync();
            var appointments = allAppointments.Where(a => a.CreatedAt >= query.StartDate && a.CreatedAt <= query.EndDate);

            // Filter by doctor if specified
            if (query.DoctorId.HasValue)
            {
                appointments = appointments.Where(a => a.DoctorId == query.DoctorId.Value);
            }

            var appointmentsList = appointments.ToList();

            // Get all cycles for revenue calculation
            var allCycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var cycles = allCycles.Where(c => c.CreatedAt >= query.StartDate && c.CreatedAt <= query.EndDate);

            if (query.DoctorId.HasValue)
            {
                cycles = cycles.Where(c => c.DoctorId == query.DoctorId.Value);
            }

            var cyclesList = cycles.ToList();

            // Get all reviews for satisfaction calculation
            var allReviews = await _unitOfWork.Reviews.GetAllAsync();
            var reviews = allReviews.Where(r => r.CreatedAt >= query.StartDate && r.CreatedAt <= query.EndDate);

            if (query.DoctorId.HasValue)
            {
                reviews = reviews.Where(r => r.DoctorId == query.DoctorId.Value);
            }

            var reviewsList = reviews.ToList();

            // Calculate average appointment duration (using scheduled duration as approximation)
            var appointmentDurations = new List<double>();
            foreach (var appointment in appointmentsList)
            {
                // Use a default duration of 30 minutes for appointments
                appointmentDurations.Add(30.0);
            }
            var averageAppointmentDuration = appointmentDurations.Any() ? appointmentDurations.Average() : 0;

            // Calculate doctor utilization rate (simplified calculation)
            var doctorUtilizationRate = 0.0;
            if (query.DoctorId.HasValue)
            {
                var doctorSchedules = await _unitOfWork.DoctorSchedules.GetSchedulesByDoctorAndDateAsync(query.DoctorId.Value, DateTime.Today);

                if (doctorSchedules.Any())
                {
                    var totalScheduledHours = doctorSchedules.Sum(ds =>
                    {
                        return (ds.EndTime - ds.StartTime).TotalHours;
                    });

                    // Assume each appointment takes 0.5 hours
                    var totalAppointmentHours = appointmentsList.Count * 0.5;

                    doctorUtilizationRate = totalScheduledHours > 0 ? (totalAppointmentHours / totalScheduledHours) * 100 : 0;
                }
            }
            else
            {
                // Calculate overall utilization rate for all doctors
                var allDoctorSchedules = await _unitOfWork.DoctorSchedules.GetSchedulesByDoctorAndDateAsync(0, DateTime.Today);
                var totalScheduledHours = allDoctorSchedules.Sum(ds =>
                {
                    return (ds.EndTime - ds.StartTime).TotalHours;
                });

                // Assume each appointment takes 0.5 hours
                var totalAppointmentHours = appointmentsList.Count * 0.5;

                doctorUtilizationRate = totalScheduledHours > 0 ? (totalAppointmentHours / totalScheduledHours) * 100 : 0;
            }

            // Calculate patient satisfaction score
            var patientSatisfactionScore = reviewsList.Any() ? reviewsList.Average(r => r.Rating) : 0;

            // Calculate total cycles completed
            var totalCyclesCompleted = cyclesList.Count(c => c.Status == CycleStatus.Completed);

            // Calculate average revenue per cycle
            var packages = await _unitOfWork.TreatmentPackages.GetAllAsync();
            var completedCycles = cyclesList.Where(c => c.Status == CycleStatus.Completed).ToList();

            var totalRevenue = completedCycles.Sum(c =>
            {
                var package = packages.FirstOrDefault(p => p.Id == c.PackageId);
                return package?.Price ?? 0;
            });

            var averageRevenuePerCycle = completedCycles.Any() ? totalRevenue / completedCycles.Count : 0;

            return new EfficiencyMetrics
            {
                AverageAppointmentDuration = averageAppointmentDuration,
                DoctorUtilizationRate = doctorUtilizationRate,
                PatientSatisfactionScore = patientSatisfactionScore,
                TotalCyclesCompleted = totalCyclesCompleted,
                AverageRevenuePerCycle = averageRevenuePerCycle
            };
        }

        public async Task<PatientJourneyResultDto> GetPatientJourneyAnalyticsAsync(PatientJourneyDto filters)
        {
            // Get all customers and users
            var customers = await _unitOfWork.Customers.GetAllWithUserAsync();
            var users = await _unitOfWork.Users.GetAllAsync();

            // Determine which patients to analyze
            var patientIds = new List<int>();
            if (filters.PatientId.HasValue)
            {
                patientIds.Add(filters.PatientId.Value);
            }
            else
            {
                // Get all patients who have cycles in the date range
                var allCycles = await _unitOfWork.TreatmentCycles.GetAllAsync();
                var relevantCycles = allCycles.Where(c => c.CreatedAt >= filters.StartDate && c.CreatedAt <= filters.EndDate);

                if (filters.DoctorId.HasValue)
                {
                    relevantCycles = relevantCycles.Where(c => c.DoctorId == filters.DoctorId.Value);
                }

                patientIds = relevantCycles.Select(c => c.CustomerId).Distinct().ToList();
            }

            if (!patientIds.Any())
            {
                return new PatientJourneyResultDto
                {
                    PatientId = 0,
                    PatientName = "No patients found",
                    Steps = new List<JourneyStepDto>()
                };
            }

            // For now, we'll analyze the first patient if multiple are found
            var targetPatientId = patientIds.First();
            var targetCustomer = customers.FirstOrDefault(c => c.Id == targetPatientId);
            var targetUser = targetCustomer?.User ?? users.FirstOrDefault(u => u.Id == targetCustomer?.UserId);

            var patientName = targetUser?.FullName ?? $"Patient {targetPatientId}";

            // Get all relevant data for the patient
            var allCyclesData = await _unitOfWork.TreatmentCycles.GetAllAsync();
            var patientCycles = allCyclesData.Where(c => c.CustomerId == targetPatientId).ToList();

            var allAppointments = await _unitOfWork.Appointments.GetAllAsync();
            var patientCycleIds = patientCycles.Select(c => c.Id).ToList();
            var patientAppointments = allAppointments
                        .Where(a => a.CycleId.HasValue && patientCycleIds.Contains(a.CycleId.Value))
                        .ToList();

            var allTestResults = await _unitOfWork.TestResults.GetAllAsync();
            var patientTestResults = allTestResults.Where(tr => patientCycles.Select(c => c.Id).Contains(tr.CycleId)).ToList();

            // Get prescriptions through treatment phases
            var allPrescriptions = await _unitOfWork.Prescriptions.GetAllAsync();
            var allPhases = await _unitOfWork.TreatmentPhases.GetAllAsync();
            var patientPhaseIds = allPhases.Where(p => patientCycles.Select(c => c.Id).Contains(p.CycleId)).Select(p => p.Id).ToList();
            var patientPrescriptions = allPrescriptions.Where(p => patientPhaseIds.Contains(p.PhaseId)).ToList();

            // Get payments through treatment packages

            var patientPackageIds = patientCycles.Select(c => c.PackageId).ToList();


            // Build journey steps
            var journeySteps = new List<JourneyStepDto>();

            // Add cycle creation steps
            foreach (var cycle in patientCycles.OrderBy(c => c.CreatedAt))
            {
                journeySteps.Add(new JourneyStepDto
                {
                    StepName = $"Treatment Cycle {cycle.Id} Started",
                    Timestamp = cycle.CreatedAt,
                    Status = cycle.Status.ToString()
                });

                if (cycle.ActualStartDate.HasValue)
                {
                    journeySteps.Add(new JourneyStepDto
                    {
                        StepName = $"Cycle {cycle.Id} Started",
                        Timestamp = cycle.ActualStartDate.Value,
                        Status = "Started"
                    });
                }

                if (cycle.ActualEndDate.HasValue)
                {
                    journeySteps.Add(new JourneyStepDto
                    {
                        StepName = $"Cycle {cycle.Id} Completed",
                        Timestamp = cycle.ActualEndDate.Value,
                        Status = "Completed"
                    });
                }
            }

            // Add appointment steps
            foreach (var appointment in patientAppointments.OrderBy(a => a.CreatedAt))
            {
                journeySteps.Add(new JourneyStepDto
                {
                    StepName = $"Appointment {appointment.Id} - {appointment.AppointmentType}",
                    Timestamp = appointment.CreatedAt,
                    Status = appointment.Status.ToString()
                });

                journeySteps.Add(new JourneyStepDto
                {
                    StepName = $"Appointment {appointment.Id} Scheduled",
                    Timestamp = appointment.ScheduledDateTime,
                    Status = "Scheduled"
                });
            }

            // Add test result steps
            foreach (var testResult in patientTestResults.OrderBy(tr => tr.CreatedAt))
            {
                journeySteps.Add(new JourneyStepDto
                {
                    StepName = $"Test Result {testResult.Id} - {testResult.TestType}",
                    Timestamp = testResult.CreatedAt,
                    Status = testResult.Status.ToString()
                });
            }

            // Add prescription steps
            foreach (var prescription in patientPrescriptions.OrderBy(p => p.CreatedAt))
            {
                journeySteps.Add(new JourneyStepDto
                {
                    StepName = $"Prescription {prescription.Id} Created",
                    Timestamp = prescription.CreatedAt,
                    Status = "Created"
                });
            }



            // Sort all steps by timestamp
            var sortedSteps = journeySteps.OrderBy(s => s.Timestamp).ToList();

            return new PatientJourneyResultDto
            {
                PatientId = targetPatientId,
                PatientName = patientName,
                Steps = sortedSteps
            };
        }

        public async Task<PredictiveAnalyticsResultDto> GetPredictiveAnalyticsAsync(PredictiveQueryDto query)
        {
            // This is a placeholder implementation for predictive analytics
            // In a real implementation, this would use machine learning models
            return new PredictiveAnalyticsResultDto
            {
                PredictionType = query.PredictionType,
                Predictions = new Dictionary<string, double>(),
                Notes = "Predictive analytics not yet implemented"
            };
        }

        public async Task<CustomReportResultDto> GenerateCustomReportAsync(CustomReportDto dto)
        {
            // This is a placeholder implementation for custom report generation
            // In a real implementation, this would generate reports based on the provided criteria
            return new CustomReportResultDto
            {
                FileUrl = "/reports/custom-report.pdf",
                FileName = $"custom-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
                ExportFormat = dto.ExportFormat
            };
        }

        public async Task<byte[]> ExportReportToPdfAsync(ExportReportDto exportRequest)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4, 25, 25, 30, 30);
                    var writer = PdfWriter.GetInstance(document, memoryStream);

                    document.Open();

                    // Title
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    var title = new Paragraph($"{exportRequest.ReportType} Report", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    document.Add(title);

                    // Date range
                    var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    var startDate = exportRequest.StartDate ?? DateTime.Now.AddMonths(-1);
                    var endDate = exportRequest.EndDate ?? DateTime.Now;
                    var dateRange = new Paragraph($"Date Range: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}", dateFont);
                    dateRange.Alignment = Element.ALIGN_CENTER;
                    document.Add(dateRange);

                    document.Add(new Paragraph("\n"));

                    switch (exportRequest.ReportType.ToLower())
                    {
                        case "revenue":
                            await AddRevenueReportToPdf(document, exportRequest);
                            break;
                        case "patient-demographics":
                            await AddPatientDemographicsToPdf(document, exportRequest);
                            break;
                        case "treatment-success":
                            await AddTreatmentSuccessRatesToPdf(document, exportRequest);
                            break;
                        default:
                            document.Add(new Paragraph("Report type not supported"));
                            break;
                    }

                    document.Close();
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error generating PDF report: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ExportReportToExcelAsync(ExportReportDto exportRequest)
        {
            try
            {
                // Set license for EPPlus 8+
                if (ExcelPackage.LicenseContext == OfficeOpenXml.LicenseContext.Commercial)
                {
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                }

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add($"{exportRequest.ReportType} Report");

                    // Header
                    worksheet.Cells[1, 1].Value = $"{exportRequest.ReportType} Report";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 16;

                    var startDate = exportRequest.StartDate ?? DateTime.Now.AddMonths(-1);
                    var endDate = exportRequest.EndDate ?? DateTime.Now;
                    worksheet.Cells[2, 1].Value = $"Date Range: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
                    worksheet.Cells[2, 1].Style.Font.Italic = true;

                    var currentRow = 4;

                    switch (exportRequest.ReportType.ToLower())
                    {
                        case "revenue":
                            currentRow = await AddRevenueReportToExcel(worksheet, exportRequest, currentRow);
                            break;
                        case "patient-demographics":
                            currentRow = await AddPatientDemographicsToExcel(worksheet, exportRequest, currentRow);
                            break;
                        case "treatment-success":
                            currentRow = await AddTreatmentSuccessRatesToExcel(worksheet, exportRequest, currentRow);
                            break;
                        default:
                            worksheet.Cells[currentRow, 1].Value = "Report type not supported";
                            break;
                    }

                    worksheet.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error generating Excel report: {ex.Message}", ex);
            }
        }

        private async Task AddRevenueReportToPdf(Document document, ExportReportDto exportRequest)
        {
            var filter = new RevenueFilterDto
            {
                StartDate = exportRequest.StartDate ?? DateTime.Now.AddMonths(-1),
                EndDate = exportRequest.EndDate ?? DateTime.Now
            };

            var report = await GetRevenueReportAsync(filter);

            // Summary
            var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            document.Add(new Paragraph("Revenue Summary", summaryFont));

            var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            document.Add(new Paragraph($"Total Revenue: ${report.TotalRevenue:N2}", contentFont));
            document.Add(new Paragraph($"Monthly Revenue: ${report.MonthlyRevenue:N2}", contentFont));
            document.Add(new Paragraph($"Yearly Revenue: ${report.YearlyRevenue:N2}", contentFont));

            document.Add(new Paragraph("\n"));

            // Service breakdown table
            if (report.ServiceBreakdown.Any())
            {
                document.Add(new Paragraph("Service Breakdown", summaryFont));

                var table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 40f, 20f, 20f, 20f });

                // Headers
                table.AddCell(new PdfPCell(new Phrase("Service", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Revenue", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Bookings", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Percentage", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Data
                foreach (var service in report.ServiceBreakdown)
                {
                    table.AddCell(service.ServiceName);
                    table.AddCell(new PdfPCell(new Phrase($"${service.Revenue:N2}")) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(service.BookingCount.ToString())) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase($"{service.Percentage:F2}%")) { HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                document.Add(table);
            }
        }

        private async Task AddPatientDemographicsToPdf(Document document, ExportReportDto exportRequest)
        {
            var dateRange = new DateRangeDto
            {
                StartDate = exportRequest.StartDate ?? DateTime.Now.AddMonths(-1),
                EndDate = exportRequest.EndDate ?? DateTime.Now
            };
            var demographics = await GetPatientDemographicsAsync(dateRange);

            var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            document.Add(new Paragraph("Patient Demographics", summaryFont));

            var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            document.Add(new Paragraph($"Total Patients: {demographics.TotalPatients}", contentFont));

            document.Add(new Paragraph("\n"));

            // Gender Distribution
            document.Add(new Paragraph("Gender Distribution", summaryFont));
            document.Add(new Paragraph($"Male: {demographics.GenderDistribution.Male}", contentFont));
            document.Add(new Paragraph($"Female: {demographics.GenderDistribution.Female}", contentFont));
            document.Add(new Paragraph($"Other: {demographics.GenderDistribution.Other}", contentFont));

            document.Add(new Paragraph("\n"));

            // Treatment Types
            if (demographics.TreatmentTypes.Any())
            {
                document.Add(new Paragraph("Treatment Types", summaryFont));

                var table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 50f, 25f, 25f });

                // Headers
                table.AddCell(new PdfPCell(new Phrase("Treatment Type", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Count", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Percentage", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Data
                foreach (var treatment in demographics.TreatmentTypes)
                {
                    table.AddCell(treatment.TreatmentType);
                    table.AddCell(new PdfPCell(new Phrase(treatment.Count.ToString())) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase($"{treatment.Percentage:F2}%")) { HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                document.Add(table);
            }
        }

        private async Task AddTreatmentSuccessRatesToPdf(Document document, ExportReportDto exportRequest)
        {
            var filter = new SuccessRateFilterDto();
            var pagination = new PaginationQueryDTO { PageNumber = 1, PageSize = 100 };
            var successRates = await GetTreatmentSuccessRatesAsync(filter, pagination);

            var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            document.Add(new Paragraph("Treatment Success Rates", summaryFont));

            if (successRates.Items.Any())
            {
                var table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 40f, 20f, 20f, 20f });

                // Headers
                table.AddCell(new PdfPCell(new Phrase("Treatment Type", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Total Cycles", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Successful", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Success Rate", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Data
                foreach (var rate in successRates.Items)
                {
                    table.AddCell(rate.TreatmentType);
                    table.AddCell(new PdfPCell(new Phrase(rate.TotalCycles.ToString())) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(rate.SuccessfulCycles.ToString())) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase($"{rate.SuccessRate:F2}%")) { HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                document.Add(table);
            }
        }

        private async Task<int> AddRevenueReportToExcel(ExcelWorksheet worksheet, ExportReportDto exportRequest, int startRow)
        {
            var filter = new RevenueFilterDto
            {
                StartDate = exportRequest.StartDate ?? DateTime.Now.AddMonths(-1),
                EndDate = exportRequest.EndDate ?? DateTime.Now
            };

            var report = await GetRevenueReportAsync(filter);

            var currentRow = startRow;

            // Summary
            worksheet.Cells[currentRow, 1].Value = "Revenue Summary";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Total Revenue:";
            worksheet.Cells[currentRow, 2].Value = report.TotalRevenue;
            worksheet.Cells[currentRow, 2].Style.Numberformat.Format = "$#,##0.00";
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Monthly Revenue:";
            worksheet.Cells[currentRow, 2].Value = report.MonthlyRevenue;
            worksheet.Cells[currentRow, 2].Style.Numberformat.Format = "$#,##0.00";
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Yearly Revenue:";
            worksheet.Cells[currentRow, 2].Value = report.YearlyRevenue;
            worksheet.Cells[currentRow, 2].Style.Numberformat.Format = "$#,##0.00";
            currentRow += 2;

            // Service breakdown
            if (report.ServiceBreakdown.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Service Breakdown";
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;

                // Headers
                worksheet.Cells[currentRow, 1].Value = "Service";
                worksheet.Cells[currentRow, 2].Value = "Revenue";
                worksheet.Cells[currentRow, 3].Value = "Bookings";
                worksheet.Cells[currentRow, 4].Value = "Percentage";

                var headerRange = worksheet.Cells[currentRow, 1, currentRow, 4];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                currentRow++;

                foreach (var service in report.ServiceBreakdown)
                {
                    worksheet.Cells[currentRow, 1].Value = service.ServiceName;
                    worksheet.Cells[currentRow, 2].Value = service.Revenue;
                    worksheet.Cells[currentRow, 2].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[currentRow, 3].Value = service.BookingCount;
                    worksheet.Cells[currentRow, 4].Value = service.Percentage / 100;
                    worksheet.Cells[currentRow, 4].Style.Numberformat.Format = "0.00%";
                    currentRow++;
                }
            }

            return currentRow;
        }

        private async Task<int> AddPatientDemographicsToExcel(ExcelWorksheet worksheet, ExportReportDto exportRequest, int startRow)
        {
            var dateRange = new DateRangeDto
            {
                StartDate = exportRequest.StartDate ?? DateTime.Now.AddMonths(-1),
                EndDate = exportRequest.EndDate ?? DateTime.Now
            };
            var demographics = await GetPatientDemographicsAsync(dateRange);

            var currentRow = startRow;

            // Summary
            worksheet.Cells[currentRow, 1].Value = "Patient Demographics";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Total Patients:";
            worksheet.Cells[currentRow, 2].Value = demographics.TotalPatients;
            currentRow += 2;

            // Gender Distribution
            worksheet.Cells[currentRow, 1].Value = "Gender Distribution";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Male:";
            worksheet.Cells[currentRow, 2].Value = demographics.GenderDistribution.Male;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Female:";
            worksheet.Cells[currentRow, 2].Value = demographics.GenderDistribution.Female;
            currentRow++;

            worksheet.Cells[currentRow, 1].Value = "Other:";
            worksheet.Cells[currentRow, 2].Value = demographics.GenderDistribution.Other;
            currentRow += 2;

            // Treatment Types
            if (demographics.TreatmentTypes.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "Treatment Types";
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;

                // Headers
                worksheet.Cells[currentRow, 1].Value = "Treatment Type";
                worksheet.Cells[currentRow, 2].Value = "Count";
                worksheet.Cells[currentRow, 3].Value = "Percentage";

                var headerRange = worksheet.Cells[currentRow, 1, currentRow, 3];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                currentRow++;

                foreach (var treatment in demographics.TreatmentTypes)
                {
                    worksheet.Cells[currentRow, 1].Value = treatment.TreatmentType;
                    worksheet.Cells[currentRow, 2].Value = treatment.Count;
                    worksheet.Cells[currentRow, 3].Value = treatment.Percentage / 100;
                    worksheet.Cells[currentRow, 3].Style.Numberformat.Format = "0.00%";
                    currentRow++;
                }
            }

            return currentRow;
        }

        private async Task<int> AddTreatmentSuccessRatesToExcel(ExcelWorksheet worksheet, ExportReportDto exportRequest, int startRow)
        {
            var filter = new SuccessRateFilterDto();
            var pagination = new PaginationQueryDTO { PageNumber = 1, PageSize = 100 };
            var successRates = await GetTreatmentSuccessRatesAsync(filter, pagination);

            var currentRow = startRow;

            worksheet.Cells[currentRow, 1].Value = "Treatment Success Rates";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            if (successRates.Items.Any())
            {
                // Headers
                worksheet.Cells[currentRow, 1].Value = "Treatment Type";
                worksheet.Cells[currentRow, 2].Value = "Total Cycles";
                worksheet.Cells[currentRow, 3].Value = "Successful";
                worksheet.Cells[currentRow, 4].Value = "Success Rate";

                var headerRange = worksheet.Cells[currentRow, 1, currentRow, 4];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                currentRow++;

                foreach (var rate in successRates.Items)
                {
                    worksheet.Cells[currentRow, 1].Value = rate.TreatmentType;
                    worksheet.Cells[currentRow, 2].Value = rate.TotalCycles;
                    worksheet.Cells[currentRow, 3].Value = rate.SuccessfulCycles;
                    worksheet.Cells[currentRow, 4].Value = rate.SuccessRate / 100;
                    worksheet.Cells[currentRow, 4].Style.Numberformat.Format = "0.00%";
                    currentRow++;
                }
            }

            return currentRow;
        }

        public async Task<byte[]> ExportTreatmentOutcomesAsync(OutcomeAnalysisDto filters, string format)
        {
            try
            {
                var result = await GetTreatmentOutcomesAsync(filters);
                var csv = $"SuccessRate\n{result.Items.FirstOrDefault()?.SuccessRate ?? 0}"
                    + "\n";
                return System.Text.Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
                var error = $"Error exporting Treatment Outcomes: {ex.Message}";
                return System.Text.Encoding.UTF8.GetBytes(error);
            }
        }

        public async Task<byte[]> ExportEfficiencyMetricsAsync(EfficiencyQueryDto query, string format)
        {
            try
            {
                var result = await GetEfficiencyMetricsAsync(query);
                var csv = $"AverageAppointmentDuration,DoctorUtilizationRate,PatientSatisfactionScore,TotalCyclesCompleted,AverageRevenuePerCycle\n"
                    + $"{result.AverageAppointmentDuration},{result.DoctorUtilizationRate},{result.PatientSatisfactionScore},{result.TotalCyclesCompleted},{result.AverageRevenuePerCycle}\n";
                return System.Text.Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
                var error = $"Error exporting Efficiency Metrics: {ex.Message}";
                return System.Text.Encoding.UTF8.GetBytes(error);
            }
        }

        public async Task<byte[]> ExportPatientJourneyAsync(PatientJourneyDto filters, string format)
        {
            try
            {
                var result = await GetPatientJourneyAnalyticsAsync(filters);
                var csv = "StepName,Timestamp,Status\n";
                foreach (var step in result.Steps)
                {
                    csv += $"{step.StepName},{step.Timestamp},{step.Status}\n";
                }
                return System.Text.Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
                var error = $"Error exporting Patient Journey: {ex.Message}";
                return System.Text.Encoding.UTF8.GetBytes(error);
            }
        }

        public async Task<byte[]> ExportPredictiveAnalyticsAsync(PredictiveQueryDto query, string format)
        {
            try
            {
                var result = await GetPredictiveAnalyticsAsync(query);
                var csv = "PredictionType,Notes\n";
                csv += $"{result.PredictionType},{result.Notes}\n";
                foreach (var kv in result.Predictions)
                {
                    csv += $"{kv.Key},{kv.Value}\n";
                }
                return System.Text.Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
                var error = $"Error exporting Predictive Analytics: {ex.Message}";
                return System.Text.Encoding.UTF8.GetBytes(error);
            }
        }

        public async Task<byte[]> ExportCustomReportAsync(CustomReportDto dto, string format)
        {
            try
            {
                var result = await GenerateCustomReportAsync(dto);
                var csv = $"FileName,ExportFormat\n{result.FileName},{result.ExportFormat}\n";
                return System.Text.Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
                var error = $"Error exporting Custom Report: {ex.Message}";
                return System.Text.Encoding.UTF8.GetBytes(error);
            }
        }

        public async Task<bool> CheckIsDoctorIdWithUserId(int userId, int doctorId)
        {
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null || user.Doctor?.Id == doctorId) return true;
            return false;
        }
    }
}
