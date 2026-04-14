using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Text.Json;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.UniversalCommands.Core;

[Export]
internal sealed class UniversalCommandsService
{
    private readonly DisposableSemaphore _disposableSemaphore = new();
    private readonly string _pluginDataFolder;
    private bool _ignoreCommandsChanges;

    [ImportingConstructor]
    public UniversalCommandsService(IPluginInfo pluginInfo)
    {
        _pluginDataFolder = pluginInfo.GetPluginDataFolder();
        Commands.CollectionChanged += Commands_CollectionChanged;
        LoadAsync().ForgetSafely();
    }

    private void Commands_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveAsync().ForgetSafely();
    }

    internal ObservableCollection<UniversalCommand> Commands { get; } = new();

    private async Task SaveAsync()
    {
        try
        {
            if (_ignoreCommandsChanges)
            {
                return;
            }

            using IDisposable _ = await _disposableSemaphore.WaitAsync(CancellationToken.None);
            string commandsFilePath = Path.Combine(_pluginDataFolder, "commands.json");
            using FileStream fileStream = File.Create(commandsFilePath);
            await JsonSerializer.SerializeAsync(fileStream, Commands.ToArray());
        }
        catch
        {
        }
    }

    private async Task LoadAsync()
    {
        string commandsFilePath = Path.Combine(_pluginDataFolder, "commands.json");
        if (File.Exists(commandsFilePath))
        {
            using FileStream fileStream = File.OpenRead(commandsFilePath);
            UniversalCommand[]? commands = await JsonSerializer.DeserializeAsync<UniversalCommand[]>(fileStream);
            if (commands is not null)
            {
                _ignoreCommandsChanges = true;
                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    foreach (UniversalCommand command in commands)
                    {
                        Commands.Add(command);
                    }
                });
                _ignoreCommandsChanges = false;

                await SaveAsync();
            }
        }
    }
}
