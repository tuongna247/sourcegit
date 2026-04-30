namespace SourceGit.Models
{
    public class PullRequest
    {
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string SourceBranch { get; set; } = string.Empty;
        public string TargetBranch { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public int CommentCount { get; set; }
    }
}
