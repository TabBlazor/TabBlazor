using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TabBlazor;

public static class MarkdownServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MarkdigMarkdownRenderer"/> as the app's <see cref="IMarkdownRenderer"/>, which turns on the
    /// preview of every <see cref="MarkdownEditor"/>.
    /// </summary>
    public static IServiceCollection AddTabBlazorMarkdown(this IServiceCollection services, Action<MarkdownRendererOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MarkdownRendererOptions();
        configure?.Invoke(options);
        services.Replace(ServiceDescriptor.Singleton<IMarkdownRenderer>(new MarkdigMarkdownRenderer(options)));
        return services;
    }
}
