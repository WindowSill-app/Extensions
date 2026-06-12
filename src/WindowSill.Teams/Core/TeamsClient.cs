using System.Net.WebSockets;
using System.Text.Json;
using WindowSill.API;
using WindowSill.Teams.Core.Models;

namespace WindowSill.Teams.Core;

internal sealed class TeamsClient : IDisposable
{
    internal static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly TeamsClientOptions _options;

    private ClientWebSocket _webSocket;
    private MeetingUpdate? _lastUpdate;
    private int _nextRequestId = 0;
    private bool _disposedValue;

    internal TeamsClient(TeamsClientOptions options)
    {
        _options = options;
        _webSocket = new ClientWebSocket();
    }

    internal event EventHandler<MeetingUpdate>? Update;

    internal event EventHandler<string>? NewToken;

    internal event EventHandler<ServiceResponse>? ServiceResponse;

    public void Dispose()
    {
        if (!_disposedValue)
        {
            _webSocket.Dispose();
            _disposedValue = true;
        }

        GC.SuppressFinalize(this);
    }

    internal async Task ConnectPerpetuallyAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            ThreadHelper.ThrowIfOnUIThread();

            while (!cancellationToken.IsCancellationRequested && _webSocket.State != WebSocketState.Open)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
                    {
                        _webSocket.Dispose();
                        _webSocket = new ClientWebSocket();
                    }

                    await _webSocket.ConnectAsync(_options.SocketUri, cancellationToken);

                    await ReadUntilCancelledAsync(cancellationToken);
                }
                catch (Exception)
                {
                    // Ignore, will retry
                    await Task.Delay(5_000, cancellationToken);
                }
            }
        });
    }

    internal Task<int> ToggleMuteAsync(CancellationToken cancellationToken)
    {
        return CallServiceAsync("toggle-mute", cancellationToken);
    }

    internal Task<int> ToggleRaiseHandAsync(CancellationToken cancellationToken)
    {
        return CallServiceAsync("toggle-hand", cancellationToken);
    }

    internal Task<int> ToggleRecordingAsync(CancellationToken cancellationToken)
    {
        return CallServiceAsync("toggle-recording", cancellationToken);
    }

    internal Task<int> ToggleVideoAsync(CancellationToken cancellationToken)
    {
        return CallServiceAsync("toggle-video", cancellationToken);
    }

    internal Task<int> LeaveCallAsync(CancellationToken cancellationToken)
    {
        return CallServiceAsync("leave-call", cancellationToken);
    }

    private async Task<int> CallServiceAsync(string action, CancellationToken cancellationToken, object? parameters = default)
    {
        _nextRequestId++;

        // If the socket isn't currently open (Teams was closed, or the connection dropped and
        // ConnectPerpetuallyAsync is re-establishing it), there's nothing to send.
        if (_webSocket.State != WebSocketState.Open)
        {
            return _nextRequestId;
        }

        var message = new ServiceRequest(action, _nextRequestId, parameters);
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, message, SerializerOptions, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.TryGetBuffer(out ArraySegment<byte> buffer))
        {
            try
            {
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch (Exception ex) when (ex is WebSocketException or IOException or InvalidOperationException)
            {
                // The remote host (Microsoft Teams) forcibly closed the connection, or it dropped
                // between the state check above and the send. ConnectPerpetuallyAsync will reconnect,
                // so treat this transient failure as a no-op instead of surfacing it as an error.
            }
        }

        return _nextRequestId;
    }

    private async Task ReadUntilCancelledAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result;
                using var memoryStream = new MemoryStream();
                do
                {
                    result = await _webSocket.ReceiveAsync(buffer, cancellationToken);
                    await memoryStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                } while (!result.EndOfMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);
                TeamsMessage? message = await JsonSerializer.DeserializeAsync<TeamsMessage>(memoryStream, SerializerOptions, cancellationToken: cancellationToken);
                if (message?.MeetingUpdate is not null)
                {
                    if (!message.MeetingUpdate.Equals(_lastUpdate))
                    {
                        Update?.Invoke(this, message.MeetingUpdate);
                        _lastUpdate = message.MeetingUpdate;
                    }
                }

                if (!string.IsNullOrWhiteSpace(message?.TokenRefresh))
                {
                    NewToken?.Invoke(this, message.TokenRefresh);
                }

                if (message?.RequestId is not null && message?.Response is not null)
                {
                    ServiceResponse?.Invoke(
                        this,
                        new ServiceResponse
                        {
                            RequestId = message.RequestId.Value,
                            Response = message.Response
                        });
                }
            }

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cancelled", CancellationToken.None);
        }
        catch (TaskCanceledException)
        {
            // ignore, it's send by the user
        }
        catch (Exception e)
        {
            Console.WriteLine("Error reading from socket {0}", e.Message);
        }
    }
}
