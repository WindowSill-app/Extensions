namespace WindowSill.Teams.Core.Models;

/// <summary>
/// Create new ServiceRequest
/// </summary>
/// <param name="action">Action to call</param>
/// <param name="requestId">Request ID</param>
/// <param name="additionalData">Data to be send as Parameters</param>
internal sealed class ServiceRequest(string action, int requestId = 0, object? additionalData = null)
{
    /// <summary>
    /// Action to call
    /// </summary>
    public string Action { get; set; } = action;

    /// <summary>
    /// Request ID
    /// </summary>
    public int RequestId { get; set; } = requestId;

    /// <summary>
    /// Additional parameters for service call
    /// </summary>
    public object Parameters { get; set; } = additionalData ?? new { };
}
