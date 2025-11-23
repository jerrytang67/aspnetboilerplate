using Autofac;
using Autofac.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Abp.AspNetCore.Dependency;

public static class AbpAutofacHostBuilderExtensions
{
    /// <summary>
    /// Uses AutofacServiceProviderFactory as service provider factory with given container
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    public static IHostBuilder UseAutofac(this IHostBuilder hostBuilder, [NotNull] IContainer container)
    {
        Check.NotNull(container, nameof(container));

        return hostBuilder
            .ConfigureServices(services =>
            {
                services.AddSingleton(container);
            })
            .UseServiceProviderFactory(new AutofacServiceProviderFactory());
    }
}
