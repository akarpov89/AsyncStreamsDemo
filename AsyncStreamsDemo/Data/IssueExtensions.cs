using System;

namespace AsyncStreamsDemo.Data
{
  public static class IssueExtensions
  {
    public static string Categorize(this Issue issue)
    {
      var resolution = "";
      var subsystem = "";
      var languageFeature = "";

      if (issue.Labels != null)
      {
        foreach (var label in issue.Labels)
        {
          if (label.StartsWith("resolution-", StringComparison.OrdinalIgnoreCase))
          {
            resolution = label.Substring("resolution-".Length);
          }
          else if (label.StartsWith("area-", StringComparison.OrdinalIgnoreCase))
          {
            subsystem = label.Substring("area-".Length);
          }
          else if (label.StartsWith("ide-", StringComparison.OrdinalIgnoreCase))
          {
            subsystem = label.Substring("ide-".Length);
          }
          else if (label.StartsWith("new feature - ", StringComparison.OrdinalIgnoreCase))
          {
            languageFeature = label.Substring("new feature - ".Length);
          }
          else if (label.StartsWith("new language feature - ", StringComparison.OrdinalIgnoreCase))
          {
            languageFeature = label.Substring("new language feature - ".Length);
          }
        }
      }

      if (!string.IsNullOrEmpty(languageFeature)) return languageFeature;
      if (!string.IsNullOrEmpty(subsystem)) return subsystem;

      return resolution;
    }
  }
}