using FluentValidation;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Business.Services;
using InfertilityTreatment.Business.Helpers;
using InfertilityTreatment.Business.Validators;
using InfertilityTreatment.Business.Mappings;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Entity.Constants;
using InfertilityTreatment.Entity.DTOs.Auth;
using InfertilityTreatment.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

            services.AddScoped<ITreatmentPhaseRepository, TreatmentPhaseRepository>();
            services.AddScoped<ITreatmentPhaseService, TreatmentPhaseService>();
            services.AddScoped<ICycleService, CycleService>();

            services.AddScoped<ITestResultRepository, TestResultRepository>();
            services.AddScoped<ITestResultService, TestResultService>();

            services.AddScoped<IMedicationRepository, MedicationRepository>();
            services.AddScoped<IMedicationService, MedicationService>();

            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            services.AddScoped<IPrescriptionService, PrescriptionService>();

            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IReviewRepository, ReviewRepository>();

            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationEventService, NotificationEventService>();
            services.AddScoped<IRealTimeNotificationService, SignalRNotificationService>();

            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentLogRepository, PaymentLogRepository>();

            // Week 6 Foundation Services
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IBookingService, BookingService>();

            // System Integration & Optimization Services
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IQueryOptimizationService, QueryOptimizationService>();
            services.AddMemoryCache();
            services.AddHttpClient();

            // Business Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDoctorService, DoctorService>();

            // SignalR Services for Real-time Notifications
            //services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

            // Helpers
            services.AddScoped<JwtHelper>();

            // AutoMapper
            services.AddAutoMapper(typeof(AutoMapperProfile));

            // FluentValidation
            services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestValidator>();
            services.AddScoped<IValidator<InfertilityTreatment.Entity.DTOs.Users.CreateUserDto>, CreateUserDtoValidator>();
            
            // Add validators for new DTOs
            services.AddScoped<IValidator<InfertilityTreatment.Entity.DTOs.TreatmentCycles.CreateCycleDto>, CreateCycleDtoValidator>();
            services.AddScoped<IValidator<InfertilityTreatment.Entity.DTOs.TreatmentCycles.InitializeCycleDto>, InitializeCycleDtoValidator>();
            services.AddScoped<IValidator<InfertilityTreatment.Entity.DTOs.TreatmentCycles.StartTreatmentDto>, StartTreatmentDtoValidator>();
            
            // Add validators for notification DTOs
            services.AddScoped<IValidator<InfertilityTreatment.Entity.DTOs.Notifications.BroadcastNotificationDto>, BroadcastNotificationDtoValidator>();
            services.AddScoped<IValidator<InfertilityTreatment.Entity.DTOs.Notifications.ScheduleNotificationDto>, ScheduleNotificationDtoValidator>();

            return services;
        }

        /// <summary>
        /// Configure Payment Gateway settings
        /// </summary>
        public static IServiceCollection AddPaymentGateways(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PaymentGatewayConfig>(configuration.GetSection("PaymentGateways"));
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

                // Handle SignalR authentication - extract token from query string for SignalR connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        // Log for debugging
                        Console.WriteLine($"[JWT] OnMessageReceived - Path: {path}, Token present: {!string.IsNullOrEmpty(accessToken)}");
                        
                        // If the request is for SignalR hub and we have an access token
                        if (!string.IsNullOrEmpty(accessToken) && 
                            (path.StartsWithSegments("/notificationHub") || path.StartsWithSegments("/hubs")))
                        {
                            context.Token = accessToken;
                            Console.WriteLine($"[JWT] Token set for SignalR connection: {accessToken.ToString().Substring(0, Math.Min(20, accessToken.ToString().Length))}...");
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[JWT] Authentication failed: {context.Exception?.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"[JWT] Token validated successfully for user: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    }
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
