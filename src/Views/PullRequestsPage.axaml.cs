using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class PullRequestsPage : UserControl
    {
        public PullRequestsPage()
        {
            InitializeComponent();
        }

        private async void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PullRequestsPage vm)
                await vm.RefreshAsync();
        }

        private void OnPRDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border { DataContext: Models.PullRequest pr } &&
                DataContext is ViewModels.PullRequestsPage vm)
                vm.OpenInBrowser(pr);
        }
    }
}
