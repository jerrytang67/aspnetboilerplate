using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Abp.AspNetCore.EmbeddedResources;
using Abp.AspNetCore.Localization;
using Abp.Dependency;
using Abp.Localization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Abp.AspNetCore.ExceptionHandling;
using Abp.AspNetCore.Security;
using Abp.AspNetCore.Uow;
using Microsoft.Extensions.Hosting;

namespace Abp.AspNetCore;

public static class AbpApplicationBuilderExtensions {
    private const string AuthorizationExceptionHandlingMiddlewareMarker = "_AbpAuthorizationExceptionHandlingMiddleware_Added";

    public static void UseAbp(this IApplicationBuilder app) {
        app.UseAbp(null);
    }

    public static void UseAbp([NotNull] this IApplicationBuilder app, Action<AbpApplicationBuilderOptions> optionsAction) {
        Check.NotNull(app, nameof(app));

        var options = new AbpApplicationBuilderOptions();
        optionsAction?.Invoke(options);

        if (options.UseCastleLoggerFactory) {
            app.UseCastleLoggerFactory();
        }

        InitializeAbp(app);

        if (options.UseAbpRequestLocalization) {
            //TODO: This should be added later than authorization middleware!
            app.UseAbpRequestLocalization();
        }

        if (options.UseSecurityHeaders) {
            app.UseAbpSecurityHeaders();
        }
    }

    public static void UseEmbeddedFiles(this IApplicationBuilder app) {
        app.UseStaticFiles(
            new StaticFileOptions {
                FileProvider = new EmbeddedResourceFileProvider(
                    app.ApplicationServices.GetRequiredService<IIocResolver>()
                )
            }
        );
    }

    private static void InitializeAbp(IApplicationBuilder app) {
        var abpBootstrapper = app.ApplicationServices.GetRequiredService<AbpBootstrapper>();
        abpBootstrapper.Initialize();

        var applicationLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopping.Register(() => abpBootstrapper.Dispose());
    }

    public static void UseCastleLoggerFactory(this IApplicationBuilder app) {
        // TODO: Castle logging facility integration has been removed during Autofac migration
        // This method is kept for backwards compatibility but does nothing
        // Use Microsoft.Extensions.Logging directly instead
    }

    public static void UseAbpRequestLocalization(this IApplicationBuilder app, Action<RequestLocalizationOptions> optionsAction = null) {
        var iocResolver = app.ApplicationServices.GetRequiredService<IIocResolver>();
        using (var languageManager = iocResolver.ResolveAsDisposable<ILanguageManager>()) {
            var supportedCultures = languageManager.Object
                .GetActiveLanguages()
                .Select(l => CultureInfo.GetCultureInfo(l.Name))
                .ToArray();

            if (iocResolver.IsRegistered<ILogger<RequestLocalizationOptions>>()) {
                using (var logger = iocResolver.ResolveAsDisposable<ILogger<RequestLocalizationOptions>>()) {
                    logger.Object.LogInformation($"Supported Request Localization Cultures: {string.Join(",", supportedCultures.Select(c => c))}");
                }
            }

            var options = new RequestLocalizationOptions {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            var userProvider = new AbpUserRequestCultureProvider(
                iocResolver.Resolve<ILoggerFactory>().CreateLogger(typeof(AbpUserRequestCultureProvider))
            );

            //0: QueryStringRequestCultureProvider
            options.RequestCultureProviders.Insert(1, userProvider);
            options.RequestCultureProviders.Insert(2, new AbpLocalizationHeaderRequestCultureProvider(
                iocResolver.Resolve<ILoggerFactory>().CreateLogger(typeof(AbpLocalizationHeaderRequestCultureProvider))
            ));
            //3: CookieRequestCultureProvider
            //4: AcceptLanguageHeaderRequestCultureProvider
            options.RequestCultureProviders.Insert(5, new AbpDefaultRequestCultureProvider(
                iocResolver.Resolve<ILoggerFactory>().CreateLogger(typeof(AbpDefaultRequestCultureProvider))
            ));

            optionsAction?.Invoke(options);

            userProvider.CookieProvider = options.RequestCultureProviders.OfType<CookieRequestCultureProvider>().FirstOrDefault();
            userProvider.HeaderProvider = options.RequestCultureProviders.OfType<AbpLocalizationHeaderRequestCultureProvider>().FirstOrDefault();

            app.UseRequestLocalization(options);
        }
    }

    public static void UseAbpSecurityHeaders(this IApplicationBuilder app) {
        app.UseMiddleware<AbpSecurityHeadersMiddleware>();
    }

    public static IApplicationBuilder UseUnitOfWork(this IApplicationBuilder app) {
        return app
            .UseMiddleware<AbpUnitOfWorkMiddleware>();
    }

    public static IApplicationBuilder UseAbpAuthorizationExceptionHandling(this IApplicationBuilder app) {
        if (app.Properties.ContainsKey(AuthorizationExceptionHandlingMiddlewareMarker)) {
            return app;
        }

        app.Properties[AuthorizationExceptionHandlingMiddlewareMarker] = true;
        return app.UseMiddleware<AbpAuthorizationExceptionHandlingMiddleware>();
    }
}