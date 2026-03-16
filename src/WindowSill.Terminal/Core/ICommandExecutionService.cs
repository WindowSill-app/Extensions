namespace WindowSill.Terminal.Core;

/// <summary>
/// Manages the lifecycle of a single command execution.
/// </summary>
public interface ICommandExecutionService
{
    /// <summary>
    /// Starts a command in the specified shell and working directory.
    /// </summary>
    /// <param name="command">The full command text to execute.</param>
    /// <param name="shell">The shell to use.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="onOutputLine">Callback invoked for each line of stdout/stderr.</param>
    /// <param name="cancellationToken">Token to cancel (kill) the process.</param>
    /// <returns>The process exit code.</returns>
    Task<int> ExecuteAsync(
        string command,
        ShellInfo shell,
        string workingDirectory,
        Action<string> onOutputLine,
        CancellationToken cancellationToken);

    /// <summary>
    /// Starts a command elevated (as administrator) in the specified shell and working directory.
    /// </summary>
    /// <remarks>
    /// The elevated process runs hidden. Output is captured via a temporary file
    /// that is tail-read during execution, since Windows does not allow direct
    /// stdout/stderr redirection from a process launched with the "runas" verb.
    /// </remarks>
    /// <param name="command">The full command text to execute.</param>
    /// <param name="shell">The shell to use.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="onOutputLine">Callback invoked for each line of output.</param>
    /// <param name="cancellationToken">Token to cancel (kill) the process.</param>
    /// <returns>The process exit code.</returns>
    Task<int> ExecuteElevatedAsync(
        string command,
        ShellInfo shell,
        string workingDirectory,
        Action<string> onOutputLine,
        CancellationToken cancellationToken);
}
