using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TabBlazor;

public class TabBlazorBuilder
{
    private readonly IServiceCollection services;

    public TabBlazorBuilder(IServiceCollection services) => this.services = services;

    public TabBlazorBuilder AddValidation<T>() where T : class, IFormValidator
    {
        services.Replace(ServiceDescriptor.Transient<IFormValidator, T>());
        return this;
    }
}