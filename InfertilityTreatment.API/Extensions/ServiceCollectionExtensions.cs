using FluentValidation;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Business.Services;
using InfertilityTreatment.Business.Helpers;
using InfertilityTreatment.Business.Validators;
using InfertilityTreatment.Business.Mappings;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Entity.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InfertilityTreatment.Service.Services;
using InfertilityTreatment.Repository;

namespace InfertilityTreatment.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register repositories, services and business logic dependencies
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Repository Pattern
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ITreatmentCycleRepository, TreatmentCycleRepository>();

            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICustomerService, CustomerService>();

            services.AddScoped<ITreatmentServiceRepository, TreatmentServiceRepository>();
            services.AddScoped<ITreatmentServiceService, TreatmentServiceService>();

            services.AddScoped<ITreatmentPackageRepository, TreatmentPackageRepository>();
            services.AddScoped<ITreatmentPackageService, TreatmentPackageService>();

            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IAppointmentService, AppointmentService>();

            services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
            services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();

            services.AddScoped<ITestResultRepository, TestResultRepository>();
            services.AddScoped<ITestResultService, TestResultService>();

            services.AddScoped<ITreatmentPhaseRepository, TreatmentPhaseRepository>();
            services.AddScoped<ICycleService, CycleService>();
            // Business Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDoctorService, DoctorService>();

            // Helpers
            services.AddScoped<JwtHelper>();

            // AutoMapper
            services.AddAutoMapper(typeof(AutoMapperProfile));

            // FluentValidation
            services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestValidator>();

            return services;
        }

        /// <summary>
        /// Configure JWT Authentication with authorization policies
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // For development
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Authorization Policies
            services.AddAuthorizationBuilder()
                .AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"))
                .AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor"))
                .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "Manager"));

            return services;
        }
    }
}
