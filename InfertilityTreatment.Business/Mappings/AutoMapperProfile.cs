using AutoMapper;
using InfertilityTreatment.Entity.DTOs.Auth;
using InfertilityTreatment.Entity.DTOs.Doctors;
using InfertilityTreatment.Entity.DTOs.TreatmentCycles;
using InfertilityTreatment.Entity.DTOs.TreatmentPakages;
using InfertilityTreatment.Entity.DTOs.TreatmentPhase;
using InfertilityTreatment.Entity.DTOs.TreatmentServices;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.DTOs.Appointments;
using InfertilityTreatment.Entity.DTOs.DoctorSchedules;
using InfertilityTreatment.Entity.DTOs.Results;

namespace InfertilityTreatment.Business.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<User, UserProfileDto>();
            CreateMap<RegisterRequestDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            // Customer mappings
            CreateMap<Customer, CustomerDetailDto>()
               .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User)).ReverseMap();
            
            CreateMap<RegisterRequestDto, Customer>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            // Doctor mappings
            CreateMap<Doctor, DoctorResponseDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName));
            CreateMap<Doctor, DoctorDetailDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.SuccessRate, opt => opt.MapFrom(src => src.SuccessRate))
                .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber));
            CreateMap<UpdateDoctorDto, Doctor>()
                .ForMember(dest => dest.User, opt => opt.Ignore()); 
            CreateMap<UpdateDoctorDto, User>();
            CreateMap<CreateDoctorDto, Doctor>();

            // Response mappings
            CreateMap<User, LoginResponseDto>()
                .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
            // Map UpdateProfile to User entity
            CreateMap<UpdateProfileDto, User>().ReverseMap();

            //TreatmentService
            CreateMap<TreatmentService, TreatmentServiceDto>();
            CreateMap<CreateTreatmentServiceDto, TreatmentService>();
            CreateMap<UpdateTreatmentServiceDto, TreatmentService>();
            CreateMap<CreateCycleDto, TreatmentCycle>();
            CreateMap<TreatmentCycle, CycleResponseDto>().ReverseMap();
            CreateMap<TreatmentCycle, CycleDetailDto>().ReverseMap();
            CreateMap<TreatmentCycle, UpdateCycleDto>().ReverseMap();
            CreateMap<TreatmentCycle, CycleResponseDto>()
            .ForMember(dest => dest.Phase, opt => opt.MapFrom(src => src.TreatmentPhases));

            //TreatmentPakage
            CreateMap<TreatmentPackage, TreatmentPackageDto>();
            CreateMap<CreateTreatmentPackageDto, TreatmentPackage>();
            CreateMap<UpdateTreatmentPackageDto, TreatmentPackage>();

            // CustomerProfile
            CreateMap<Customer, CustomerProfileDto>();

            //TreatmentPhase
            CreateMap<TreatmentPhase, PhaseResponseDto>().ReverseMap();
            CreateMap<TreatmentPhase, CreatePhaseDto>().ReverseMap();
            CreateMap<TreatmentPhase, UpdatePhaseDto>().ReverseMap();

            //Appointment
            CreateMap<Appointment, AppointmentDto>();
            CreateMap<CreateAppointmentDto, Appointment>();
            CreateMap<UpdateAppointmentDto, Appointment>();

            //DoctorSchedule
            CreateMap<DoctorSchedule, DoctorScheduleDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.TimeOfDay))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.TimeOfDay));
            CreateMap<CreateDoctorScheduleDto, DoctorSchedule>();
            CreateMap<UpdateDoctorScheduleDto, DoctorSchedule>();

            //TestResult
            CreateMap<TestResult, TestResultDto>();
            CreateMap<TestResult, TestResultDetailDto>();
            CreateMap<CreateTestResultDto, TestResult>();
            CreateMap<UpdateTestResultDto, TestResult>();
        }
    }
}
