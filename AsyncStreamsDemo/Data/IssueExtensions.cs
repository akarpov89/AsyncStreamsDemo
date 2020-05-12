using System;

namespace AsyncStreamsDemo.Data
{
  public static class IssueExtensions
  {
    public static string Categorize(this Issue issue)
    {
      const string areaPrefix = "area-";
      const string idePrefix = "ide-";
      const string newFeaturePrefix = "new feature - ";
      const string newLanguageFeaturePrefix = "new language feature - ";

      var subsystem = "";
      var languageFeature = "";

      if (issue.Labels != null)
      {
        foreach (var label in issue.Labels)
        {
          if (label.StartsWith(areaPrefix, StringComparison.OrdinalIgnoreCase))
          {
            subsystem = label.Substring(areaPrefix.Length);
          }
          else if (label.StartsWith(idePrefix, StringComparison.OrdinalIgnoreCase))
          {
            subsystem = label.Substring(idePrefix.Length);
          }
          else if (label.StartsWith(newFeaturePrefix, StringComparison.OrdinalIgnoreCase))
          {
            languageFeature = label.Substring(newFeaturePrefix.Length);
          }
          else if (label.StartsWith(newLanguageFeaturePrefix, StringComparison.OrdinalIgnoreCase))
          {
            languageFeature = label.Substring(newLanguageFeaturePrefix.Length);
          }
        }
      }

      if (!string.IsNullOrEmpty(languageFeature)) return languageFeature;
      if (!string.IsNullOrEmpty(subsystem)) return subsystem;

      return "";
    }
  }
}