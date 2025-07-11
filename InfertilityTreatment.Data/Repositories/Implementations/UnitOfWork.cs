using Microsoft.EntityFrameworkCore.Storage;
using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;

namespace InfertilityTreatment.Data.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repository instances
        private IUserRepository? _users;
        private ICustomerRepository? _customers;
        private IDoctorRepository? _doctors;
        private IRefreshTokenRepository? _refreshTokens;
        
        // Treatment Related Repository instances
        private ITreatmentServiceRepository? _treatmentServices;
        private ITreatmentPackageRepository? _treatmentPackages;
        private ITreatmentCycleRepository? _treatmentCycles;
        private ITreatmentPhaseRepository? _treatmentPhases;
        
        // Appointment & Scheduling Repository instances
        private IAppointmentRepository? _appointments;
        private IDoctorScheduleRepository? _doctorSchedules;
        
        // Medical & Monitoring Repository instances
        private ITestResultRepository? _testResults;
        private IMedicationRepository? _medications;
        private IPrescriptionRepository? _prescriptions;
        
        // Content & Communication Repository instances
        private IReviewRepository? _reviews;
        private INotificationRepository? _notifications;


        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lazy loading repositories
        public IUserRepository Users => _users ??= new UserRepository(_context);
        public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
        public IDoctorRepository Doctors => _doctors ??= new DoctorRepository(_context);
        public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
        
        // Treatment Related Repositories
        public ITreatmentServiceRepository TreatmentServices => _treatmentServices ??= new TreatmentServiceRepository(_context);
        public ITreatmentPackageRepository TreatmentPackages => _treatmentPackages ??= new TreatmentPackageRepository(_context);
        public ITreatmentCycleRepository TreatmentCycles => _treatmentCycles ??= new TreatmentCycleRepository(_context);
        public ITreatmentPhaseRepository TreatmentPhases => _treatmentPhases ??= new TreatmentPhaseRepository(_context);
        
        // Appointment & Scheduling Repositories
        public IAppointmentRepository Appointments => _appointments ??= new AppointmentRepository(_context);
        public IDoctorScheduleRepository DoctorSchedules => _doctorSchedules ??= new DoctorScheduleRepository(_context);
        
        // Medical & Monitoring Repositories
        public ITestResultRepository TestResults => _testResults ??= new TestResultRepository(_context);
        public IMedicationRepository Medications => _medications ??= new MedicationRepository(_context);
        public IPrescriptionRepository Prescriptions => _prescriptions ??= new PrescriptionRepository(_context);
        
        // Content & Communication Repositories
        public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
        public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);


        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
