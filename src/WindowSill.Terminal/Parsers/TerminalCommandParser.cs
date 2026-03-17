using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace WindowSill.Terminal.Parsers;

internal class TerminalCommandParser
{
    private static readonly string[] executableExtensions = [".exe", ".cmd", ".bat", ".ps1", ".com"];

    /// <summary>
    /// Given the full text selected by the user, returns whether it contains a bash command that can be executed in WSL.
    /// </summary>
    /// <param name="selectedText">The full text selected by the user.</param>
    /// <returns></returns>
    internal static bool HasWslHint(string selectedText)
    {
        // Not implemented, needs WSL ShellInfo support
        // TODO: Needs to be aware of available WSL installs.
        string? mdCodeBlockLanguage = GetFirstMarkdownCodeBlockLanguage(selectedText);
        return mdCodeBlockLanguage is "wsl" or "bash";
    }

    internal static bool HasPowerShellHint(string selectedText)
    {
        // `powershell` Windows PowerShell (5, netfx) cannot be disambiguated from `pwsh` PowerShell (7+, netx) using the command prefix (PS) alone.
        // Additional well-known disambiguating formats must be contained in the text selected by the user.
        string? mdCodeBlockLanguage = GetFirstMarkdownCodeBlockLanguage(selectedText);
        return mdCodeBlockLanguage == "powershell";
    }

    internal static bool HasPwshHint(string selectedText)
    {
        // `powershell` Windows PowerShell (5, netfx) cannot be disambiguated from `pwsh` PowerShell (7+, netx) using the command prefix (PS) alone.
        // Additional well-known disambiguating formats must be contained in the text selected by the user.
        string? mdCodeBlockLanguage = GetFirstMarkdownCodeBlockLanguage(selectedText);
        return mdCodeBlockLanguage == "pwsh";
    }

    internal static bool HasCmdHint(string selectedText)
    {
        string? mdCodeBlockLanguage = GetFirstMarkdownCodeBlockLanguage(selectedText);
        return mdCodeBlockLanguage is "cmd" or "batch";
    }

    /// <summary>
    /// Given the full text selected by the user, returns the first markdown code block language definition found.
    /// </summary>
    /// <param name="selectedText">The full text selected by the user.</param>
    internal static string? GetFirstMarkdownCodeBlockLanguage(string selectedText)
    {
        // Get individual lines (without clobbering between newline types)
        string[] lines = selectedText.Split(["\r\n", "\n\n", "\n"], StringSplitOptions.None);

        // Iterate lines for delimiter
        string delimiter = "```";
        foreach (string line in lines)
        {
            // A code block may not always appear at the start of a new line in markdown
            // Code blocks can also appear inside of lists, blockquotes, and tables. 
            // Instead of asserting the start of the line, only check if exists and capture to end of line.
            if (line.Contains(delimiter))
            {
                return line.Split(delimiter, StringSplitOptions.None)[1];
            }
        }

        return null;
    }

    /// <summary>
    /// Given the fulltext selected by the user, parses and returns the first inferred working directory from the text. 
    /// </summary>
    /// <param name="selectedText">The full text selected by the user.</param>
    internal static string? GetFirstWorkingDirectory(string selectedText)
    {
        WindowsHostCommandLineInputEntry? winHostCommandLineInput = ParseWindowsHostCommandLines(selectedText, shellDelimiter: "PS", commandDelimiter: ">").FirstOrDefault();

        return winHostCommandLineInput?.WorkingDirectory ?? GetBashWorkingDirectory(selectedText);
    }

    /// <summary>
    /// Given the fulltext selected by the user, parses and returns the first inferred terminal command from the text. 
    /// </summary>
    /// <param name="selectedText">The full text selected by the user.</param>
    internal static string? GetFirstTerminalCommand(string selectedText)
    {
        WindowsHostCommandLineInputEntry? winHostCommandLineInput = ParseWindowsHostCommandLines(selectedText, shellDelimiter: "PS", commandDelimiter: ">").FirstOrDefault();

        return winHostCommandLineInput?.TerminalCommand ?? GetBashTerminalCommand(selectedText);
    }

    /// <summary>
    /// Iterates the selected text and separates out individual powershell invocations.
    /// </summary>
    /// <remarks>
    /// Does not handle multiline commands, to be added.
    /// </remarks>
    /// <param name="selectedText">The full text selected by the user.</param>
    internal static IEnumerable<WindowsHostCommandLineInputEntry> ParseWindowsHostCommandLines(string selectedText, string shellDelimiter, string commandDelimiter)
    {
        //
        // TODO: How are multiple multiline commands parsed? Either:
        // 1) The PS and > symbols are delimiters between separate commands, implying multiline selections are single commands.
        // 2) The multiline selections are always treated as separate commands, but then no multiline command support.
        // 3) The multiline selections are only treated as separate commands when a valid command separator is used, but then that implies 1) is preferred over 2).
        // The options laid out above point towards using what each shell defines as a command separator between the shell-specific input prefixes (PS, bash shebang) to hard-separate multiple selected commands.
        //
        // With that figured out, we're cleared to examine how to make "multiple selected commands" work throughout the code-- it's simple for parsing, it's complex for appending the result inline. 
        // We don't support direct "run and replace inline" yet, so we'll return to this later. Rationale is preserved.  
        // 

        // Find the first line that contains the PS prefix
        // Get individual lines (without clobbering between newline types)
        string[] lines = selectedText.Split(["\r\n", "\n\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        // Iterate lines
        foreach (string line in lines)
        {
            // If no command delimiter is present, no working directory can be declared.
            // Early exit, return entire line as terminal command.
            if (!line.Contains(commandDelimiter))
            {
                // Raw line text without a command delimiter may result in non-commands being executed as code.
                // To know whether this line contains a real command, we must check whether it's an executable binary on PATH.
                string fileNameWithOrWithoutExtension = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
                bool isExecutableBinary = IsExecutableBinaryOnEnvironmentPath(fileNameWithOrWithoutExtension, executableExtensions);

                if (isExecutableBinary)
                {
                    yield return new(null, line);
                }

                continue;
            }

            // Assume the working directory may be declared at start of line
            // then check/split it out
            string startsAtWorkingPath = line;

            // Any shell indicator MUST be at the start of a selected line, not arbitrarily within it.
            if (line.StartsWith(shellDelimiter))
            {
                // Remove shell prefix
                startsAtWorkingPath = line.Split(shellDelimiter, StringSplitOptions.TrimEntries)[1];
            }

            // Slice by the commandDelimiter between the cwd and the actual command 
            string[] beforeAndAfterCommandDelimiter = startsAtWorkingPath.Split(commandDelimiter, StringSplitOptions.TrimEntries);

            // Before the symbol without PS prefix only contains the cwd.
            // After the symbol only contains the command.
            string beforeCommandDelimiter = beforeAndAfterCommandDelimiter[0];
            string afterCommandDelimiter = beforeAndAfterCommandDelimiter[1];

            // Only the selected commands which don't include a command and/or shell delimitor need to be checked for presence on PATH. 
            // With an explicit shell hint or shell delimitor, the selected command may be an interpreted shell function and may pass through.
            yield return new(beforeCommandDelimiter, afterCommandDelimiter);
        }

        yield break;
    }

    /// <summary>
    /// Represents a single Windows-style command as input into a terminal.
    /// </summary>
    /// <remarks>
    /// May represent both CMD and PS commands.
    /// </remarks>
    internal record WindowsHostCommandLineInputEntry(string? WorkingDirectory, string TerminalCommand);

    internal static string? GetBashTerminalCommand(string selectedText)
    {
        // Not implemented, needs WSL ShellInfo support
        return null;
    }

    internal static string? GetBashWorkingDirectory(string selectedText)
    {
        // Not implemented, needs WSL ShellInfo support
        return null;
    }

    internal static string? GetBashMachineName(string selectedText)
    {
        // Not implemented, needs WSL ShellInfo support
        return null;
    }

    internal static string? GetBashUserName(string selectedText)
    {
        // Not implemented, needs WSL ShellInfo support
        return null;
    }

    /// <summary>
    /// Given text that may contain a binary name and command arguments, this method checks whether the binary is available on PATH.
    /// </summary>
    /// <param name="selectedText">The full text that the user has selected.</param>
    /// <param name="executableExtensions">The file extensions that signal an executable binary.</param>
    /// <returns>True if the binary is found, otherwise false.</returns>
    internal static bool IsExecutableBinaryOnEnvironmentPath(string fileNameWithOrWithoutExtension, string[] executableExtensions)
    {
        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathValue))
        {
            return false;
        }

        string[] directories = pathValue.Split(Path.PathSeparator);

        foreach (string directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            // Check the token as-is (may already include an extension).
            if (File.Exists(Path.Combine(directory, fileNameWithOrWithoutExtension)))
            {
                return true;
            }

            // Check with each known executable extension.
            foreach (string ext in executableExtensions)
            {
                if (File.Exists(Path.Combine(directory, fileNameWithOrWithoutExtension + ext)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
