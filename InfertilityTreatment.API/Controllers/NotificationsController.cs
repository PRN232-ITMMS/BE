using Microsoft.AspNetCore.Mvc;
using InfertilityTreatment.Entity.DTOs.Notifications;
using InfertilityTreatment.Business.Interfaces;
using System.Security.Claims;
using InfertilityTreatment.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.Entities;


namespace InfertilityTreatment.API.Controllers
{
    [ApiController]
    [Route("api/notifications/")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IBaseRepository<User> _userRepository;

        public NotificationsController(INotificationService notificationService, IBaseRepository<User> userRepository)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<NotificationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated or ID not found in claims."
                });
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value);

            if (notifications == null || !notifications.Any())
            {
                return Ok(new
                {
                    success = true,
                    message = "No notifications found for this user.",
                    data = new List<NotificationResponseDto>()
                });
            }
            return Ok(new { success = true, message = "Notifications retrieved successfully.", data = notifications });
        }

        [HttpPut("{id}/mark-read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated or ID not found in claims."
                });
            }

            var notification = await _notificationService.GetNotificationIfOwnedAsync(id, userId.Value);
            if (notification == null || notification.UserId != userId.Value)
                return NotFound(new
                {
                    success = false,
                    message = "You do not have permission to mark this notification as read or notification not found."
                });

            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Notification with ID {id} not found or could not be marked as read."
                });
            }
            return Ok(new { success = true, message = $"Notification {id} marked as read successfully." });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated or ID not found in claims."
                });
            }

            var success = await _notificationService.DeleteNotificationAsync(id);
            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Notification with ID {id} not found or could not be deleted."
                });
            }
            return Ok(new { success = true, message = $"Notification {id} deleted successfully." });
        }
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = nameof(UserRole.Doctor))]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createDto)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            if (currentUserId == null || currentUserRole == null)
            {
                return Unauthorized(new { success = false, message = "User not authenticated or role not found." });
            }
            if (currentUserRole != UserRole.Doctor.ToString())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Only doctors are allowed to create notifications." });
            }

            var targetUser = await _userRepository.GetByIdAsync(createDto.UserId);
            if (targetUser == null)
            {
                return BadRequest(new { success = false, message = "Target user for notification not found." });
            }
            if (targetUser.Role != UserRole.Customer)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Doctors can only create notifications for customers." });
            }

            var success = await _notificationService.CreateNotificationAsync(createDto);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to create notification due to an internal service error."
                });
            }
            return StatusCode(StatusCodes.Status201Created,
                new { success = true, message = "Notification created successfully." });
        }
    }
}

