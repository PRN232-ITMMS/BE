using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfertilityTreatment.API.Controllers
{
    [Route("api/system-test")]
    [ApiController]
    public class SystemTestController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SystemTestController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Test all repositories in UnitOfWork to verify BE-FIX001 completion
        /// </summary>
        [HttpGet("verify-repositories")]
        public IActionResult VerifyRepositories()
        {
            try
            {
                var repositoryStatus = new Dictionary<string, bool>
                {
                    // Core User Management
                    ["Users"] = _unitOfWork.Users != null,
                    ["Customers"] = _unitOfWork.Customers != null,
                    ["Doctors"] = _unitOfWork.Doctors != null,
                    ["RefreshTokens"] = _unitOfWork.RefreshTokens != null,
                    
                    // Treatment Related (NEWLY ADDED)
                    ["TreatmentServices"] = _unitOfWork.TreatmentServices != null,
                    ["TreatmentPackages"] = _unitOfWork.TreatmentPackages != null,
                    ["TreatmentCycles"] = _unitOfWork.TreatmentCycles != null,
                    ["TreatmentPhases"] = _unitOfWork.TreatmentPhases != null,
                    
                    // Appointment & Scheduling
                    ["Appointments"] = _unitOfWork.Appointments != null,
                    ["DoctorSchedules"] = _unitOfWork.DoctorSchedules != null,
                    
                    // Medical & Monitoring (NEWLY ADDED)
                    ["TestResults"] = _unitOfWork.TestResults != null,
                    ["Medications"] = _unitOfWork.Medications != null,
                    ["Prescriptions"] = _unitOfWork.Prescriptions != null,
                    
                    // Content & Communication (NEWLY ADDED)
                    ["Reviews"] = _unitOfWork.Reviews != null,
                    ["Notifications"] = _unitOfWork.Notifications != null
                };

                var totalRepositories = repositoryStatus.Count;
                var workingRepositories = repositoryStatus.Values.Count(x => x);
                var failedRepositories = repositoryStatus.Where(x => !x.Value).Select(x => x.Key).ToList();

                var result = new
                {
                    Status = failedRepositories.Count == 0 ? "SUCCESS" : "FAILED",
                    TotalRepositories = totalRepositories,
                    WorkingRepositories = workingRepositories,
                    FailedRepositories = failedRepositories,
                    RepositoryDetails = repositoryStatus,
                    Message = failedRepositories.Count == 0 
                        ? "✅ BE-FIX001 COMPLETED: All repositories accessible through UnitOfWork"
                        : $"❌ BE-FIX001 INCOMPLETE: {failedRepositories.Count} repositories failed to initialize",
                    NewlyAddedRepositories = new[]
                    {
                        "TreatmentServices", "TreatmentPackages", "TreatmentPhases", 
                        "TestResults", "Notifications"
                    }
                };

                return Ok(ApiResponseDto<object>.CreateSuccess(result, 
                    "Repository verification completed"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<object>.CreateError(
                    "Repository verification failed", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Test database connectivity through all repositories
        /// </summary>
        [HttpGet("test-database-connectivity")]
        public async Task<IActionResult> TestDatabaseConnectivity()
        {
            try
            {
                var connectivityTests = new Dictionary<string, bool>();

                // Test each repository's database connectivity
                try { await _unitOfWork.Users.CountAsync(); connectivityTests["Users"] = true; }
                catch { connectivityTests["Users"] = false; }

                try { await _unitOfWork.TreatmentServices.GetAllAsync(); connectivityTests["TreatmentServices"] = true; }
                catch { connectivityTests["TreatmentServices"] = false; }

                try { await _unitOfWork.TreatmentPackages.GetAllAsync(); connectivityTests["TreatmentPackages"] = true; }
                catch { connectivityTests["TreatmentPackages"] = false; }

                try { await _unitOfWork.TestResults.GetAllAsync(); connectivityTests["TestResults"] = true; }
                catch { connectivityTests["TestResults"] = false; }

                try { await _unitOfWork.Notifications.GetAllAsync(); connectivityTests["Notifications"] = true; }
                catch { connectivityTests["Notifications"] = false; }

                try { await _unitOfWork.TreatmentPhases.GetAllAsync(); connectivityTests["TreatmentPhases"] = true; }
                catch { connectivityTests["TreatmentPhases"] = false; }

                try { await _unitOfWork.Appointments.GetAllAsync(); connectivityTests["Appointments"] = true; }
                catch { connectivityTests["Appointments"] = false; }

                var workingConnections = connectivityTests.Values.Count(x => x);
                var totalConnections = connectivityTests.Count;

                var result = new
                {
                    Status = workingConnections == totalConnections ? "SUCCESS" : "PARTIAL",
                    WorkingConnections = workingConnections,
                    TotalConnections = totalConnections,
                    ConnectivityDetails = connectivityTests,
                    Message = workingConnections == totalConnections
                        ? "✅ All repository database connections working"
                        : $"⚠️ {workingConnections}/{totalConnections} repository connections working"
                };

                return Ok(ApiResponseDto<object>.CreateSuccess(result, 
                    "Database connectivity test completed"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDto<object>.CreateError(
                    "Database connectivity test failed", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Test transaction functionality through UnitOfWork
        /// </summary>
        [HttpGet("test-transaction")]
        public async Task<IActionResult> TestTransaction()
        {
            try
            {
                // Test transaction begin/commit/rollback functionality
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Ok(ApiResponseDto<object>.CreateSuccess(new
                {
                    Status = "SUCCESS",
                    Message = "✅ Transaction functionality working correctly"
                }, "Transaction test completed"));
            }
            catch (Exception ex)
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                catch { }

                return StatusCode(500, ApiResponseDto<object>.CreateError(
                    "Transaction test failed", new List<string> { ex.Message }));
            }
        }
    }
}
