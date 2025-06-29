using InfertilityTreatment.API.Extensions;
using InfertilityTreatment.API.Middleware;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Business.Services;
using InfertilityTreatment.Data.Context;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using InfertilityTreatment.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUserEmailPreferenceRepository, UserEmailPreferenceRepository>();

builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<ITreatmentCycleRepository, TreatmentCycleRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ITestResultRepository, TestResultRepository>();
builder.Services.AddScoped<ITreatmentPhaseRepository, TreatmentPhaseRepository>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IEmailQueueService, EmailQueueService>();
builder.Services.AddScoped<IUserEmailPreferenceService, UserEmailPreferenceService>();

builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ITestResultService, TestResultService>();
builder.Services.AddScoped<ICycleService, CycleService>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Business Services
builder.Services.AddBusinessServices();

builder.Services.AddHostedService<EmailSenderBackgroundService>();
builder.Services.AddHostedService<ScheduledNotificationProcessorService>();


// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configure Swagger with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Infertility Treatment API",
        Version = "v1",
        Description = "API for managing infertility treatment cycles and patient care"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\\n\\nExample: \"Bearer 12345abcdef\""
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS for React app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline

// Custom middleware (order matters!)
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Development middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Infertility Treatment API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// CORS must be before Authentication
app.UseCors("AllowReactApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName,
    Version = "1.0.0"
})
.WithName("HealthCheck")
.WithTags("Health");

app.Run();
