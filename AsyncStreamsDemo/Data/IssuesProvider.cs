using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncStreamsDemo.Data
{
  public class IssuesProvider
  {
    private const string IssuesQuery =
      @"query ($owner_name: String!, $repo_name: String!,  $start_cursor:String) {
            repository(owner: $owner_name, name: $repo_name) {
            issues(last: 25, before: $start_cursor) {
              pageInfo {
                hasPreviousPage
                startCursor
              }
              nodes {
                title
                url
                number
                createdAt
                state
                author {
                  login
                  url
                }
                labels(first: 5) {
                  nodes {
                    name
                    description
                  }
                }
              }
            }
          }
        }";


    private readonly HttpClient myHttpClient;

    public IssuesProvider(HttpClient httpClient)
    {
      myHttpClient = httpClient;
      myHttpClient.DefaultRequestHeaders.Add("User-Agent", "GiHubQuery App");
    }

    public async Task<IEnumerable<Issue>> GetIssuesAsync1(
      string ownerName,
      string repoName,
      IProgress<int> progress,
      CancellationToken cancellationToken = default)
    {
      const int maxIssuesCount = 200;
      var issues = new List<Issue>();

      var request = new IssuesRequest {OwnerName = ownerName, RepoName = repoName, Query = IssuesQuery};

      while (true)
      {
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        var response = await PostAsync(request, cancellationToken).ConfigureAwait(false);

        issues.AddRange(response.Issues);
        progress?.Report(issues.Count);

        if (!response.HasMorePages || issues.Count >= maxIssuesCount)
          break;

        request.StartCursor = response.StartCursor;

        cancellationToken.ThrowIfCancellationRequested();
      }

      return issues;
    }

    public IEnumerable<Task<Issue?>> GetIssuesAsync2(
      string ownerName,
      string repoName,
      CancellationToken cancellationToken = default)
    {
      var request = new IssuesRequest {OwnerName = ownerName, RepoName = repoName, Query = IssuesQuery};
      List<Issue>? currentPage = null;
      var currentIndex = 0;

      while (true)
      {
        yield return GetNextIssueAsync();

        cancellationToken.ThrowIfCancellationRequested();
      }

      async Task<Issue?> GetNextIssueAsync()
      {
        if (currentPage == null || currentIndex >= currentPage.Count)
        {
          currentIndex = 0;
          await Task.Delay(200, cancellationToken).ConfigureAwait(false);

          var currentResponse = await PostAsync(request, cancellationToken).ConfigureAwait(false);
          currentPage = currentResponse.Issues.ToList();

          if (currentPage.Count == 0) return null;

          request.StartCursor = currentResponse.StartCursor;
        }

        return currentPage[currentIndex++];
      }
    }

    public IObservable<Issue> GetIssuesAsync3(string ownerName, string repoName)
    {
      return Observable.Create<Issue>(async (observer, cancellationToken) =>
      {
        var request = new IssuesRequest {OwnerName = ownerName, RepoName = repoName, Query = IssuesQuery};

        while (true)
        {
          await Task.Delay(200, cancellationToken).ConfigureAwait(false);

          var response = await PostAsync(request, cancellationToken).ConfigureAwait(false);

          foreach (var issue in response.Issues)
          {
            observer.OnNext(issue);
          }

          if (!response.HasMorePages)
          {
            observer.OnCompleted();
            break;
          }

          request.StartCursor = response.StartCursor;

          cancellationToken.ThrowIfCancellationRequested();
        }
      });
    }

    public async IAsyncEnumerable<Issue> GetIssuesAsync4(
      string ownerName,
      string repoName,
      CancellationToken cancellationToken = default)
    {
      var request = new IssuesRequest {OwnerName = ownerName, RepoName = repoName, Query = IssuesQuery};

      while (true)
      {
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        var response = await PostAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (var issue in response.Issues)
        {
          yield return issue;
        }

        if (!response.HasMorePages)
          yield break;

        request.StartCursor = response.StartCursor;

        cancellationToken.ThrowIfCancellationRequested();
      }
    }

    private async Task<IssuesResponse> PostAsync(IssuesRequest request, CancellationToken token)
    {
      var postBody = request.ToJsonText();

      var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("https://api.github.com/graphql"));

      httpRequestMessage.Headers.Add("Authorization", $"Token {Authorization.Token}");
      httpRequestMessage.Content = new StringContent(postBody, Encoding.UTF8, "application/json");

      var httpResponseMessage = await myHttpClient.SendAsync(httpRequestMessage, token).ConfigureAwait(false);

      httpResponseMessage.EnsureSuccessStatusCode();

      var responseBody = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

      return IssuesResponse.Parse(responseBody);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class IssuesRequest
    {
      // ReSharper disable once UnusedAutoPropertyAccessor.Local
      [JsonPropertyName("query")] public string? Query { get; set; }

      [JsonPropertyName("variables")] public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>();

      [JsonIgnore]
      public string OwnerName
      {
        get => Variables["owner_name"];
        set => Variables["owner_name"] = value;
      }

      [JsonIgnore]
      public string RepoName
      {
        get => Variables["repo_name"];
        set => Variables["repo_name"] = value;
      }

      [JsonIgnore]
      public string StartCursor
      {
        get => Variables["start_cursor"];
        set => Variables["start_cursor"] = value;
      }

      public string ToJsonText() => JsonSerializer.Serialize(this);
    }

    private class IssuesResponse
    {
      private readonly JsonElement myIssuesElement;

      private IssuesResponse(string responseBody)
      {
        var rootElement = JsonDocument.Parse(responseBody).RootElement;
        myIssuesElement = rootElement.GetProperty("data").GetProperty("repository").GetProperty("issues");
      }

      public static IssuesResponse Parse(string responseBody) => new IssuesResponse(responseBody);

      public IEnumerable<Issue> Issues
      {
        get
        {
          foreach (var issueElement in myIssuesElement.GetProperty("nodes").EnumerateArray())
          {
            yield return CreateFromJson(issueElement);
          }
        }
      }

      public bool HasMorePages => myIssuesElement.GetProperty("pageInfo").GetProperty("hasPreviousPage").GetBoolean();
      public string StartCursor => myIssuesElement.GetProperty("pageInfo").GetProperty("startCursor").GetString();

      private static Issue CreateFromJson(JsonElement issueElement)
      {
        return new Issue
        {
          Title = issueElement.GetProperty("title").GetString(),
          Url = new Uri(issueElement.GetProperty("url").GetString()),
          CreatedAt = issueElement.GetProperty("createdAt").GetDateTimeOffset(),
          State = ConvertToIssueState(issueElement.GetProperty("state").GetString()),
          Author = new Author
          {
            Login = issueElement.GetProperty("author").GetProperty("login").GetString(),
            Url = new Uri(issueElement.GetProperty("author").GetProperty("url").GetString())
          },
          Labels = ExtractLabels(issueElement.GetProperty("labels").GetProperty("nodes"))
        };

        static IssueState ConvertToIssueState(string state) => state switch
        {
          "CLOSED" => IssueState.Closed,
          "OPEN" => IssueState.Open,
          _ => IssueState.Unknown
        };

        static string[] ExtractLabels(JsonElement labelsElement)
        {
          var result = new string[labelsElement.GetArrayLength()];
          for (var index = 0; index < result.Length; index++)
          {
            result[index] = labelsElement[index].GetProperty("name").GetString();
          }

          return result;
        }
      }
    }
  }
}