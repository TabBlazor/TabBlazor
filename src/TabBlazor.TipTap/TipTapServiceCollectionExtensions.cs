using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TabBlazor;

public static class TipTapServiceCollectionExtensions
{
    /// <summary>
    /// Registers app-wide <see cref="TipTapOptions"/> for <see cref="RichTextEditor"/> and <see cref="RichTextView"/>.
    /// The editor works without it; <see cref="RichTextView"/> needs a sanitizer (or
    /// <see cref="TipTapOptions.AllowUnsanitizedHtml"/>) from here.
    /// </summary>
    public static IServiceCollection AddTabBlazorTipTap(this IServiceCollection services, Action<TipTapOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new TipTapOptions();
        configure?.Invoke(options);
        services.Replace(ServiceDescriptor.Singleton(options));
        return services;
    }
}
