namespace WindowSill.Teams.Core.Models;

internal sealed class ServiceResponse
{
    /// <summary>
    /// Request ID of the request this is a response to
    /// </summary>
    public int RequestId { get; set; }
    /// <summary>
    /// Response from the service
    /// </summary>
    public string? Response { get; set; }
}
