using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class TerminalPanel : UserControl
    {
        public TerminalPanel()
        {
            InitializeComponent();
            DataContextChanged += (_, _) =>
            {
                if (DataContext is ViewModels.TerminalSession session)
                    session.PropertyChanged += OnSessionPropertyChanged;
            };
        }

        private void OnSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModels.TerminalSession.Output))
                OutputScroll.ScrollToEnd();
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.TerminalSession session)
            {
                session.SubmitCommand();
                InputBox.Focus();
                e.Handled = true;
            }
        }

        private void OnClearClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.TerminalSession session)
                session.Clear();
        }

        private void OnRestartClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.TerminalSession session)
                session.Restart();
        }
    }
}
