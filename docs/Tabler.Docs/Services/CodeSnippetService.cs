using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Docs.Services
{

    public interface ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className);
    }

    public class FakeSnippetService : ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className)
        {
            return Task.FromResult("Source code view is disabled");
        }
    }

    public class LocalSnippetService : ICodeSnippetService
    {
        public async Task<string> GetCodeSnippet(string className)
        {
            var basePath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.FullName;
            const string projectName = "Tabler.Docs";
            var classPath = projectName + className.Substring(projectName.Length).Replace(".", @"\");
            var codePath = Path.Combine(basePath, $"{classPath}.razor");

            if (File.Exists(codePath))
            {
                return await Task.FromResult(File.ReadAllText(codePath));
            }
            else
            {
                return await Task.FromResult($"Unable to find code at {codePath}");
            }
        }
    }

    public class GitHubSnippetService : ICodeSnippetService
    {
        const string repo = "joadan/Blazor-Tabler";
        const string baseUrl = "https://raw.githubusercontent.com/joadan/Blazor-Tabler/master/docs/Tabler.Docs";
        private readonly IHttpClientFactory httpClientFactory;

        public GitHubSnippetService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }


        public async Task<string> GetCodeSnippet(string className)
        {
            try
            {
                var names = className.Split(".");
                var folder1 = names.SkipLast(2).Last();
                var folder2 = names.SkipLast(1).Last();
                var fileName = $"{names.Last()}.razor";
                var filePath = $"{baseUrl}/{folder1}/{folder2}/{fileName}";
               // Console.WriteLine($"Try to access github with path {filePath}");

                using var httpClient = httpClientFactory.CreateClient("GitHub");
                using var stream = await httpClient.GetStreamAsync(filePath);
                StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
                // return await httpClient.GetStringAsync(filePath);
            }
            catch (Exception ex)
            {
                return $"Unable to load code. Error: {ex.Message}";
            }
        }
    }
}
