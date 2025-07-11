namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository Properties
        IUserRepository Users { get; }
        ICustomerRepository Customers { get; }
        IDoctorRepository Doctors { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        
        // Treatment Related Repositories
        ITreatmentServiceRepository TreatmentServices { get; }
        ITreatmentPackageRepository TreatmentPackages { get; }
        ITreatmentCycleRepository TreatmentCycles { get; }
        ITreatmentPhaseRepository TreatmentPhases { get; }
        
        // Appointment & Scheduling Repositories
        IAppointmentRepository Appointments { get; }
        IDoctorScheduleRepository DoctorSchedules { get; }
        
        // Medical & Monitoring Repositories
        ITestResultRepository TestResults { get; }
        IMedicationRepository Medications { get; }
        IPrescriptionRepository Prescriptions { get; }
          // Content & Communication Repositories
        IReviewRepository Reviews { get; }
        INotificationRepository Notifications { get; }


        // Transaction Methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}