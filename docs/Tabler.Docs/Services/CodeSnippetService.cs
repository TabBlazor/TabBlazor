using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Tabler.Docs.Services;

public interface ICodeSnippetService
{
    public Task<string> GetCodeSnippet(string className);
    public Task<byte[]> GetSamplePDF();
}

public class FakeSnippetService : ICodeSnippetService
{
    public Task<string> GetCodeSnippet(string className)
    {
        return Task.FromResult("Source code view is disabled");
    }

    public Task<byte[]> GetSamplePDF()
    {
        throw new NotImplementedException();
    }
}

public class LocalSnippetService : ICodeSnippetService
{
    public async Task<string> GetCodeSnippet(string className)
    {
        var basePath = GetSolutionRoot();
        const string projectName = "Tabler.Docs";
        var classPath = projectName + className.Substring(projectName.Length)
            .Replace(".", Path.DirectorySeparatorChar.ToString());
        var codePath = Path.Combine(basePath, $"{classPath}.razor");

        if (File.Exists(codePath))
        {
            return await File.ReadAllTextAsync(codePath);
        }

        return $"Unable to find code at {codePath}";
    }

    public async Task<byte[]> GetSamplePDF()
    {
        var path = Path.Combine(GetSolutionRoot(), "Tabler.Docs", "wwwroot", "pdf", "sample.pdf");
        return await File.ReadAllBytesAsync(path);
    }

    private static string GetSolutionRoot()
    {
        var dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
        for (var i = 0; i < 4 && dir is not null; i++)
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Unable to resolve solution root from assembly location.");
        }

        return dir.FullName;
    }
}

public class GitHubSnippetService : ICodeSnippetService
{
    private const string baseUrl = "https://tabblazor.com/_content/razor_source";
    private readonly ConcurrentDictionary<string, string> cachedCode = new();
    private readonly IHttpClientFactory httpClientFactory;
    private readonly NavigationManager navManager;

    public GitHubSnippetService(IHttpClientFactory httpClientFactory, NavigationManager navManager)
    {
        this.httpClientFactory = httpClientFactory;
        this.navManager = navManager;
    }

    public async Task<string> GetCodeSnippet(string className)
    {
        try
        {
            if (cachedCode.TryGetValue(className, out var cached))
            {
                return cached;
            }

            var baseName = "Tabler.Docs.";
            var path = baseUrl + "/" + className.Replace(baseName, "").Replace(".", "/") + ".razor";

            using var httpClient = httpClientFactory.CreateClient("GitHub");
            var code = await httpClient.GetStringAsync(path);

            cachedCode[className] = code;
            return code;
        }
        catch (Exception ex)
        {
            return $"Unable to load code. Error: {ex.Message}";
        }
    }

    public async Task<byte[]> GetSamplePDF()
    {
        var url = navManager.BaseUri + "_content/Tabler.Docs/pdf/sample.pdf";
        using var httpClient = httpClientFactory.CreateClient("GitHub");
        var arr = await httpClient.GetByteArrayAsync(url);
        return arr;
    }
}