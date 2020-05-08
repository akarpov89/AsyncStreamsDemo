using System;
using System.Net.Http;
using System.Threading.Tasks;

using AsyncStreamsDemo.Data;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncStreamsDemo
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(args);
      builder.RootComponents.Add<App>("app");

      builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
      builder.Services.AddTransient<IssuesProvider>();

      await builder.Build().RunAsync();
    }
  }
}
