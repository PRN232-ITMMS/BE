using AutoMapper;
using Microsoft.Extensions.Configuration;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Business.Helpers;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Auth;
using InfertilityTreatment.Entity.DTOs.Email;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Enums;

namespace InfertilityTreatment.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            JwtHelper jwtHelper,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // Find user by email with related data
            var user = await _unitOfWork.Users.FindByEmailWithRelatedDataAsync(request.Email);
            
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password
            if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account has been deactivated");
            }

            // Generate tokens
            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var refreshToken = await _jwtHelper.GenerateRefreshTokenAsync(user.Id);

            // Get token expiry
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"] ?? "60");

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = _mapper.Map<UserProfileDto>(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                // Begin transaction
                await _unitOfWork.BeginTransactionAsync();

                // Check if email already exists
                if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
                {
                    return new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Email is already registered"
                    };
                }

                // Create user entity
                var user = _mapper.Map<User>(request);
                user.PasswordHash = PasswordHelper.HashPassword(request.Password);
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;

                // Add user to database
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // If role is Customer, create customer profile
                if (request.Role == UserRole.Customer)
                {
                    var customer = _mapper.Map<Customer>(request);
                    customer.UserId = user.Id;
                    customer.CreatedAt = DateTime.UtcNow;
                    customer.IsActive = true;

                    await _unitOfWork.Customers.AddAsync(customer);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Send welcome email for customer registrations
                if (request.Role == UserRole.Customer)
                {
                    try
                    {
                        // Prepare and send welcome email
                        var welcomeEmailDto = new SendWelcomeEmailDto
                        {
                            Email = user.Email,
                            CustomerName = user.FullName
                        };
                        await _emailService.SendWelcomeEmailAsync(welcomeEmailDto);
                    }
                    catch (Exception)
                    {
                        // Log email error but don't fail registration
                        // Email failures should not prevent successful registration
                    }
                }

                return new RegisterResponseDto
                {
                    Success = true,
                    Message = "Registration successful",
                    UserId = user.Id
                };
            }
            catch (Exception ex)
            {
                // Rollback transaction
                await _unitOfWork.RollbackTransactionAsync();
                
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            // Validate refresh token
            var refreshToken = await _jwtHelper.ValidateRefreshTokenAsync(request.RefreshToken);
            
            if (refreshToken == null)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Get user with related data
            var user = await _unitOfWork.Users.GetByIdWithRelatedDataAsync(refreshToken.UserId);
            
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or deactivated");
            }

            // Revoke old refresh token
            await _jwtHelper.RevokeRefreshTokenAsync(request.RefreshToken);

            // Generate new tokens
            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var newRefreshToken = await _jwtHelper.GenerateRefreshTokenAsync(user.Id);

            // Get token expiry
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"] ?? "60");

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                User = _mapper.Map<UserProfileDto>(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        public async Task LogoutAsync(int userId)
        {
            // Revoke all refresh tokens for the user
            await _jwtHelper.RevokeAllRefreshTokensAsync(userId);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var refreshToken = await _jwtHelper.ValidateRefreshTokenAsync(token);
            return refreshToken != null;
        }
    }
}
