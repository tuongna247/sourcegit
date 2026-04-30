using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Collections;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class PullRequestsPage : ObservableObject
    {
        public AvaloniaList<Models.PullRequest> PullRequests { get; } = [];

        public Models.PullRequest SelectedPR
        {
            get => _selectedPR;
            set => SetProperty(ref _selectedPR, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string PlatformLabel
        {
            get => _platformLabel;
            private set => SetProperty(ref _platformLabel, value);
        }

        public bool HasToken => !string.IsNullOrEmpty(GetToken());

        public PullRequestsPage(Repository repo)
        {
            _repo = repo;
            DetectPlatform();
        }

        public async Task RefreshAsync()
        {
            var token = GetToken();
            if (_service is null)
            {
                StatusMessage = "No GitHub/GitLab remote detected.";
                return;
            }
            if (string.IsNullOrEmpty(token))
            {
                StatusMessage = "No API token configured. Go to Preferences → Integration.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading pull requests...";

            var prs = await _service.FetchPullRequestsAsync(token);

            PullRequests.Clear();
            foreach (var pr in prs)
                PullRequests.Add(pr);

            IsLoading = false;
            StatusMessage = prs.Count == 0 ? "No open pull requests." : string.Empty;
        }

        public void OpenInBrowser(Models.PullRequest pr)
        {
            if (!string.IsNullOrEmpty(pr?.Url))
                Native.OS.OpenBrowser(pr.Url);
        }

        private void DetectPlatform()
        {
            foreach (var remote in _repo.Remotes)
            {
                var svc = Models.RemoteHostingService.Detect(remote.URL);
                if (svc is not null)
                {
                    _service = svc;
                    PlatformLabel = svc.Platform switch
                    {
                        Models.HostingPlatform.GitHub => $"GitHub — {svc.Owner}/{svc.Repo}",
                        Models.HostingPlatform.GitLab => $"GitLab — {svc.Owner}/{svc.Repo}",
                        _ => string.Empty
                    };
                    return;
                }
            }
            PlatformLabel = "No remote hosting detected";
        }

        private string GetToken()
        {
            if (_service is null) return string.Empty;
            return _service.Platform switch
            {
                Models.HostingPlatform.GitHub => Preferences.Instance.GitHubApiToken,
                Models.HostingPlatform.GitLab => Preferences.Instance.GitLabApiToken,
                _ => string.Empty
            };
        }

        private readonly Repository _repo;
        private Models.RemoteHostingService _service;
        private Models.PullRequest _selectedPR;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private string _platformLabel = string.Empty;
    }
}
