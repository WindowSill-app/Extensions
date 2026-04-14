using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.UniversalCommands.Core;
using WindowSill.UniversalCommands.Settings;

namespace WindowSill.UniversalCommands.ViewModels;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly UniversalCommandsService _universalCommandsService;

    internal SettingsViewModel(UniversalCommandsService universalCommandService)
    {
        _universalCommandsService = universalCommandService;
    }

    internal XamlRoot? XamlRoot { get; set; }

    internal bool IsEmpty => _universalCommandsService.Commands.Count == 0;

    internal ObservableCollection<UniversalCommand> Commands => _universalCommandsService.Commands;

    [RelayCommand]
    internal async Task AddAsync()
    {
        if (XamlRoot is null)
        {
            return;
        }

        UniversalCommand? newAction = await ActionEditorDialog.NewActionAsync(XamlRoot);
        if (newAction is not null)
        {
            _universalCommandsService.Commands.Insert(0, newAction);
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    internal async Task EditAsync(UniversalCommand command)
    {
        if (XamlRoot is null)
        {
            return;
        }

        UniversalCommand? edited = await ActionEditorDialog.EditActionAsync(XamlRoot, command);
        if (edited is not null)
        {
            int index = _universalCommandsService.Commands.IndexOf(command);
            _universalCommandsService.Commands[index] = edited;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    internal async Task RemoveAsync(UniversalCommand command)
    {
        _universalCommandsService.Commands.Remove(command);
        OnPropertyChanged(nameof(IsEmpty));
    }
}
