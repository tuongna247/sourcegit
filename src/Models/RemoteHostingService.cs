using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SourceGit.Models
{
    public enum HostingPlatform { Unknown, GitHub, GitLab }

    public class RemoteHostingService
    {
        public HostingPlatform Platform { get; }
        public string Owner { get; }
        public string Repo { get; }

        public static RemoteHostingService Detect(string remoteUrl)
        {
            if (string.IsNullOrEmpty(remoteUrl))
                return null;

            try
            {
                var url = NormalizeUrl(remoteUrl);
                if (string.IsNullOrEmpty(url)) return null;

                var uri = new Uri(url);
                var host = uri.Host.ToLowerInvariant();
                var parts = uri.AbsolutePath.Trim('/').Replace(".git", "").Split('/');
                if (parts.Length < 2) return null;

                if (host.Contains("github.com"))
                    return new RemoteHostingService(HostingPlatform.GitHub, parts[0], parts[1]);
                if (host.Contains("gitlab"))
                    return new RemoteHostingService(HostingPlatform.GitLab, parts[0], parts[1]);
            }
            catch { }

            return null;
        }

        public async Task<List<PullRequest>> FetchPullRequestsAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return [];

            return Platform switch
            {
                HostingPlatform.GitHub => await FetchGitHubPRsAsync(token),
                HostingPlatform.GitLab => await FetchGitLabMRsAsync(token),
                _ => []
            };
        }

        private RemoteHostingService(HostingPlatform platform, string owner, string repo)
        {
            Platform = platform;
            Owner = owner;
            Repo = repo;
        }

        private async Task<List<PullRequest>> FetchGitHubPRsAsync(string token)
        {
            var result = new List<PullRequest>();
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("User-Agent", "SourceGit");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                var url = $"https://api.github.com/repos/{Owner}/{Repo}/pulls?state=open&per_page=50";
                var rsp = await client.GetAsync(url);
                if (!rsp.IsSuccessStatusCode) return result;

                var json = await rsp.Content.ReadAsStringAsync();
                var array = JsonNode.Parse(json)?.AsArray();
                if (array is null) return result;

                foreach (var item in array)
                {
                    result.Add(new PullRequest
                    {
                        Number = item["number"]?.GetValue<int>() ?? 0,
                        Title = item["title"]?.GetValue<string>() ?? "",
                        State = item["state"]?.GetValue<string>() ?? "",
                        Author = item["user"]?["login"]?.GetValue<string>() ?? "",
                        AuthorAvatar = item["user"]?["avatar_url"]?.GetValue<string>() ?? "",
                        SourceBranch = item["head"]?["ref"]?.GetValue<string>() ?? "",
                        TargetBranch = item["base"]?["ref"]?.GetValue<string>() ?? "",
                        Url = item["html_url"]?.GetValue<string>() ?? "",
                        CreatedAt = item["created_at"]?.GetValue<string>() ?? "",
                        CommentCount = item["comments"]?.GetValue<int>() ?? 0,
                    });
                }
            }
            catch { }
            return result;
        }

        private async Task<List<PullRequest>> FetchGitLabMRsAsync(string token)
        {
            var result = new List<PullRequest>();
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);

                var encodedPath = Uri.EscapeDataString($"{Owner}/{Repo}");
                var url = $"https://gitlab.com/api/v4/projects/{encodedPath}/merge_requests?state=opened&per_page=50";
                var rsp = await client.GetAsync(url);
                if (!rsp.IsSuccessStatusCode) return result;

                var json = await rsp.Content.ReadAsStringAsync();
                var array = JsonNode.Parse(json)?.AsArray();
                if (array is null) return result;

                foreach (var item in array)
                {
                    result.Add(new PullRequest
                    {
                        Number = item["iid"]?.GetValue<int>() ?? 0,
                        Title = item["title"]?.GetValue<string>() ?? "",
                        State = item["state"]?.GetValue<string>() ?? "",
                        Author = item["author"]?["name"]?.GetValue<string>() ?? "",
                        AuthorAvatar = item["author"]?["avatar_url"]?.GetValue<string>() ?? "",
                        SourceBranch = item["source_branch"]?.GetValue<string>() ?? "",
                        TargetBranch = item["target_branch"]?.GetValue<string>() ?? "",
                        Url = item["web_url"]?.GetValue<string>() ?? "",
                        CreatedAt = item["created_at"]?.GetValue<string>() ?? "",
                        CommentCount = item["user_notes_count"]?.GetValue<int>() ?? 0,
                    });
                }
            }
            catch { }
            return result;
        }

        private static string NormalizeUrl(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;

            // git@github.com:owner/repo.git → https://github.com/owner/repo.git
            if (url.StartsWith("git@"))
            {
                var stripped = url.Substring(4);
                var colonIdx = stripped.IndexOf(':');
                if (colonIdx < 0) return null;
                var host = stripped.Substring(0, colonIdx);
                var path = stripped.Substring(colonIdx + 1);
                return $"https://{host}/{path}";
            }

            return null;
        }
    }
}
