using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NexaDesk.Models;
using NexaDesk.ViewModels;
using Windows.Graphics;

namespace NexaDesk.Views;

public sealed partial class CommandPaletteWindow : Window
{
    private readonly CommandPaletteViewModel _viewModel = new();

    public CommandPaletteWindow()
    {
        InitializeComponent();
        if (Content is FrameworkElement root)
        {
            root.DataContext = _viewModel;
        }

        Title = "NexaDesk Command Palette";
        SystemBackdrop = new MicaBackdrop();

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
    }

    public async void ShowPalette()
    {
        PositionWindow();
        AppWindow.Show();
        Activate();

        QueryBox.Text = string.Empty;
        QueryBox.Focus(FocusState.Programmatic);
        await _viewModel.SearchAsync(string.Empty);
        ResultsList.SelectedIndex = _viewModel.Results.Count > 0 ? 0 : -1;
    }

    private void PositionWindow()
    {
        const int width = 720;
        const int height = 520;

        DisplayArea area = DisplayArea.GetFromWindowId(
            AppWindow.Id,
            DisplayAreaFallback.Primary);

        RectInt32 work = area.WorkArea;
        int x = work.X + Math.Max(0, (work.Width - width) / 2);
        int y = work.Y + Math.Max(24, (work.Height - height) / 3);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private async void OnQueryChanged(object sender, TextChangedEventArgs e)
    {
        await _viewModel.SearchAsync(QueryBox.Text);
        ResultsList.SelectedIndex = _viewModel.Results.Count > 0 ? 0 : -1;
    }

    private async void OnQueryKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            AppWindow.Hide();
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Down)
        {
            ResultsList.SelectedIndex = Math.Min(
                ResultsList.SelectedIndex + 1,
                ResultsList.Items.Count - 1);
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Up)
        {
            ResultsList.SelectedIndex = Math.Max(ResultsList.SelectedIndex - 1, 0);
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter &&
            ResultsList.SelectedItem is ActionDefinition action)
        {
            await ExecuteAsync(action);
            e.Handled = true;
        }
    }

    private async void OnResultClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ActionDefinition action)
        {
            await ExecuteAsync(action);
        }
    }

    private async Task ExecuteAsync(ActionDefinition action)
    {
        ExecutionResult result = await _viewModel.ExecuteAsync(action);
        if (result.Success)
        {
            AppWindow.Hide();
        }
    }
}
