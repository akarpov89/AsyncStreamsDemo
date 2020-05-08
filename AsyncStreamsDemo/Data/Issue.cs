using System;

namespace AsyncStreamsDemo.Data
{
  public class Issue
  {
    public string? Title { get; set; }
    public Uri? Url { get; set; }
    public Author? Author { get; set; }
    public IssueState State { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string[]? Labels { get; set; }
  }

  public class Author
  {
    public string? Login { get; set; }
    public Uri? Url { get; set; }
  }

  public enum IssueState
  {
    Unknown,
    Open,
    Closed
  }
}