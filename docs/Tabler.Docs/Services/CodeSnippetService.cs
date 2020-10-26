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
        const string baseUrl = "https://raw.githubusercontent.com/joadan/Blazor-Tabler/master/docs/Tabler.Docs/Components";
        private readonly IHttpClientFactory httpClientFactory;

        public GitHubSnippetService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }


        public async Task<string> GetCodeSnippet(string className)
        {

            //return "Temporary disbled..";
            var httpClient = httpClientFactory.CreateClient("GitHub");
            //httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Blazor-Tabler", "1"));

            try
            {
                var names = className.Split(".");
                var folder = names.SkipLast(1).Last();
                var fileName = $"{names.Last()}.razor";
                var filePath = $"{baseUrl}/{folder}/{fileName}";
                return await httpClient.GetStringAsync(filePath);
            }
            catch (Exception ex)
            {
                return $"Unable to load code. Error: {ex.Message}";
            }


        }
        //public async Task<List<FileData>> GetFilesAsync(string repo, string path)
        //{
        //    var result = new List<FileData>();

        //    var httpClient = new HttpClient();
        //    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Blazor-Icons", "1"));

        //    var contentsUrl = $"https://api.github.com/repos/{repo}/contents/{path}";
        //    var contentsJson = await httpClient.GetStringAsync(contentsUrl);
        //    var contents = (JArray)JsonConvert.DeserializeObject(contentsJson);
        //    //var counter = 0;
        //    foreach (var file in contents)
        //    {
        //        //counter++;
        //        //if (counter >= 9) { return result; }
        //        var fileType = (string)file["type"];
        //        //if (fileType == "dir")
        //        //{
        //        //    var directoryContentsUrl = (string)file["url"];
        //        //    // use this URL to list the contents of the folder
        //        //    Console.WriteLine($"DIR: {directoryContentsUrl}");
        //        //}
        //        if (fileType == "file")
        //        {
        //            var fileData = new FileData();
        //            fileData.FileName = (string)file["name"];
        //            fileData.Data = await httpClient.GetByteArrayAsync((string)file["download_url"]);
        //            result.Add(fileData);
        //            Console.WriteLine($"Added: {fileData.FileName}");


        //        }
        //    }
        //    return result;
        //}

    }


}
