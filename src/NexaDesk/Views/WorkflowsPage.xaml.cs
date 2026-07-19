using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NexaDesk.Models;
using NexaDesk.ViewModels;

namespace NexaDesk.Views;

public sealed partial class WorkflowsPage : Page
{
    public WorkflowsViewModel ViewModel { get; } = new();

    public WorkflowsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadAsync();

    private async void OnCreateWorkflowClicked(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<ActionDefinition> actions =
            await App.Services.Database.SearchActionsAsync(string.Empty, 100);

        TextBox nameBox = new()
        {
            Header = "工作流名称",
            PlaceholderText = "例如：开始开发"
        };

        ListView actionList = new()
        {
            ItemsSource = actions,
            SelectionMode = ListViewSelectionMode.Multiple,
            Height = 320,
            DisplayMemberPath = nameof(ActionDefinition.Name)
        };

        StackPanel content = new() { Spacing = 12 };
        content.Children.Add(nameBox);
        content.Children.Add(new TextBlock { Text = "选择要按顺序执行的动作" });
        content.Children.Add(actionList);

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "新建工作流",
            Content = content,
            PrimaryButtonText = "创建",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        ActionDefinition[] selected =
            actionList.SelectedItems.Cast<ActionDefinition>().ToArray();

        if (string.IsNullOrWhiteSpace(nameBox.Text) || selected.Length == 0)
        {
            ViewModel.StatusMessage = "请输入名称并至少选择一个动作。";
            return;
        }

        await ViewModel.CreateAsync(nameBox.Text, selected);
    }
}
