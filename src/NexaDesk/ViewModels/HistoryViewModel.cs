using System.Collections.ObjectModel;
using NexaDesk.Models;

namespace NexaDesk.ViewModels;

public sealed class HistoryViewModel
{
    public ObservableCollection<ExecutionHistoryItem> Items { get; } = [];

    public async Task LoadAsync()
    {
        Items.Clear();
        foreach (ExecutionHistoryItem item in await App.Services.Database.GetHistoryAsync())
        {
            Items.Add(item);
        }
    }
}
