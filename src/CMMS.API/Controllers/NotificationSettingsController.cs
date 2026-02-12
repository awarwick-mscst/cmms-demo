using CMMS.API.Attributes;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
[RequiresFeature("email-calendar")]
public class NotificationSettingsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationSettingsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserNotificationPreferenceDto>>>> GetMyPreferences(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<IEnumerable<UserNotificationPreferenceDto>>.Fail("User not authenticated"));

        var preferences = await _notificationService.GetUserPreferencesAsync(userId.Value, cancellationToken);

        // Return all notification types with their preferences (default to enabled if not set)
        var allTypes = Enum.GetValues<NotificationType>();
        var result = allTypes.Select(type =>
        {
            var existing = preferences.FirstOrDefault(p => p.NotificationType == type);
            return new UserNotificationPreferenceDto
            {
                Id = existing?.Id ?? 0,
                UserId = userId.Value,
                NotificationType = type.ToString(),
                NotificationTypeDisplay = GetNotificationTypeDisplay(type),
                EmailEnabled = existing?.EmailEnabled ?? true,
                CalendarEnabled = existing?.CalendarEnabled ?? true
            };
        });

        return Ok(ApiResponse<IEnumerable<UserNotificationPreferenceDto>>.Ok(result));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<UserNotificationPreferenceDto>>> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<UserNotificationPreferenceDto>.Fail("User not authenticated"));

        if (!Enum.TryParse<NotificationType>(request.NotificationType, out var type))
            return BadRequest(ApiResponse<UserNotificationPreferenceDto>.Fail("Invalid notification type"));

        var preference = await _notificationService.SetUserPreferenceAsync(
            userId.Value, type, request.EmailEnabled, request.CalendarEnabled, cancellationToken);

        var dto = MapToDto(preference);
        return Ok(ApiResponse<UserNotificationPreferenceDto>.Ok(dto, "Preference updated successfully"));
    }

    [HttpPut("preferences/bulk")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserNotificationPreferenceDto>>>> UpdatePreferencesBulk(
        [FromBody] BulkUpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<IEnumerable<UserNotificationPreferenceDto>>.Fail("User not authenticated"));

        var results = new List<UserNotificationPreferenceDto>();

        foreach (var pref in request.Preferences)
        {
            if (!Enum.TryParse<NotificationType>(pref.NotificationType, out var type))
                continue;

            var preference = await _notificationService.SetUserPreferenceAsync(
                userId.Value, type, pref.EmailEnabled, pref.CalendarEnabled, cancellationToken);

            results.Add(MapToDto(preference));
        }

        return Ok(ApiResponse<IEnumerable<UserNotificationPreferenceDto>>.Ok(results, "Preferences updated successfully"));
    }

    [HttpGet("types")]
    public ActionResult<ApiResponse<IEnumerable<NotificationTypeInfo>>> GetNotificationTypes()
    {
        var types = Enum.GetValues<NotificationType>().Select(type => new NotificationTypeInfo
        {
            Value = type.ToString(),
            DisplayName = GetNotificationTypeDisplay(type),
            Description = GetNotificationTypeDescription(type)
        });

        return Ok(ApiResponse<IEnumerable<NotificationTypeInfo>>.Ok(types));
    }

    private static UserNotificationPreferenceDto MapToDto(UserNotificationPreference preference)
    {
        return new UserNotificationPreferenceDto
        {
            Id = preference.Id,
            UserId = preference.UserId,
            NotificationType = preference.NotificationType.ToString(),
            NotificationTypeDisplay = GetNotificationTypeDisplay(preference.NotificationType),
            EmailEnabled = preference.EmailEnabled,
            CalendarEnabled = preference.CalendarEnabled
        };
    }

    private static string GetNotificationTypeDisplay(NotificationType type) => type switch
    {
        NotificationType.WorkOrderAssigned => "Work Order Assigned",
        NotificationType.WorkOrderApproachingDue => "Work Order Approaching Due Date",
        NotificationType.WorkOrderOverdue => "Work Order Overdue",
        NotificationType.WorkOrderCompleted => "Work Order Completed",
        NotificationType.PMScheduleComingDue => "PM Schedule Coming Due",
        NotificationType.PMScheduleOverdue => "PM Schedule Overdue",
        NotificationType.LowStockAlert => "Low Stock Alert",
        _ => type.ToString()
    };

    private static string GetNotificationTypeDescription(NotificationType type) => type switch
    {
        NotificationType.WorkOrderAssigned => "Receive notification when a work order is assigned to you",
        NotificationType.WorkOrderApproachingDue => "Receive reminder before work order due date",
        NotificationType.WorkOrderOverdue => "Receive notification when work order becomes overdue",
        NotificationType.WorkOrderCompleted => "Receive notification when a work order you requested is completed",
        NotificationType.PMScheduleComingDue => "Receive reminder before PM schedule due date",
        NotificationType.PMScheduleOverdue => "Receive notification when PM schedule becomes overdue",
        NotificationType.LowStockAlert => "Receive notification when inventory falls below reorder point",
        _ => string.Empty
    };
}
