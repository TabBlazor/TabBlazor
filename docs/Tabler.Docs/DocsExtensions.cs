using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using TabBlazor;
using Tabler.Docs.Services;
using Tabler.Docs.Components.Forms.RichTextEditors;

namespace Tabler.Docs
{
    public static class DocsExtensions
    {
        public static IServiceCollection AddDocs(this IServiceCollection services)
        {
            return services
               .AddTabler(options =>
               {
                   options.AssemblyScanFilter = () => [typeof(Flags).Assembly];
                   options.EnablePopper = true;
                   options.DefaultPositioning = Positioning.Absolute;
               })
               .AddTabBlazorMarkdown(options => options.AllowedImageUrlSchemes.Add("data"))
               .AddTabBlazorTipTap(options =>
               {
                   var sanitizer = DocsRichTextSanitizer.Create();
                   options.AllowHtml(html => sanitizer.Sanitize(html));
                   options.ExtensionModules.Add("./_content/Tabler.Docs/js/rich-text-callout.js");
               })
               .AddSingleton<AppService>();
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new T[size];

                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket.Select(x => x);

                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                Array.Resize(ref bucket, count);
                yield return bucket.Select(x => x);
            }
        }
    }
}
