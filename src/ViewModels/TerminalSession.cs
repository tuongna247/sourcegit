using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class TerminalSession : ObservableObject, IDisposable
    {
        public string Output
        {
            get => _output;
            private set => SetProperty(ref _output, value);
        }

        public string InputLine
        {
            get => _inputLine;
            set => SetProperty(ref _inputLine, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set => SetProperty(ref _isRunning, value);
        }

        public string WorkingDirectory { get; }

        public TerminalSession(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            StartShell();
        }

        public void SubmitCommand()
        {
            if (_process is null || _process.HasExited)
            {
                StartShell();
                return;
            }

            var cmd = InputLine;
            InputLine = string.Empty;

            if (string.IsNullOrWhiteSpace(cmd))
                return;

            Append($"\n> {cmd}");
            _process.StandardInput.WriteLine(cmd);
        }

        public void Clear()
        {
            Output = string.Empty;
        }

        public void Restart()
        {
            _process?.Kill();
            _process?.Dispose();
            _process = null;
            Output = string.Empty;
            StartShell();
        }

        public void Dispose()
        {
            _process?.Kill();
            _process?.Dispose();
            _process = null;
        }

        private void StartShell()
        {
            IsRunning = false;

            var info = new ProcessStartInfo
            {
                WorkingDirectory = WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            if (OperatingSystem.IsWindows())
            {
                info.FileName = "cmd.exe";
                info.Arguments = "/Q /K \"prompt $G$S\"";
            }
            else if (OperatingSystem.IsMacOS())
            {
                info.FileName = "/bin/zsh";
                info.Arguments = "--login";
            }
            else
            {
                info.FileName = "/bin/bash";
                info.Arguments = "--login";
            }

            try
            {
                _process = new Process { StartInfo = info };
                _process.OutputDataReceived += (_, e) => { if (e.Data != null) Append(e.Data); };
                _process.ErrorDataReceived += (_, e) => { if (e.Data != null) Append(e.Data); };
                _process.Exited += (_, _) => Dispatcher.UIThread.Post(() => IsRunning = false);
                _process.EnableRaisingEvents = true;
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                IsRunning = true;
            }
            catch (Exception ex)
            {
                Output = $"[Failed to start shell: {ex.Message}]";
            }
        }

        private void Append(string line)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var sb = new StringBuilder(Output);
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(line);

                if (sb.Length > 65536)
                    Output = sb.ToString().Substring(sb.Length - 65536);
                else
                    Output = sb.ToString();
            });
        }

        private string _output = string.Empty;
        private string _inputLine = string.Empty;
        private bool _isRunning;
        private Process _process;
    }
}
