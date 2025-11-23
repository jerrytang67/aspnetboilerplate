using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Localization;
using Abp.Logging;

namespace Abp.AspNetCore.Localization;

public class AbpDefaultRequestCultureProvider(ILogger logger) : RequestCultureProvider {
    public override async Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext) {
        var settingManager = httpContext.RequestServices.GetRequiredService<ISettingManager>();

        var culture = await settingManager.GetSettingValueAsync(LocalizationSettingNames.DefaultLanguage);

        if (culture.IsNullOrEmpty()) {
            return null;
        }

        logger.LogDebug("{0} - Using Culture:{1} , UICulture:{2}", nameof(AbpDefaultRequestCultureProvider), culture, culture);
        return new ProviderCultureResult(culture, culture);
    }
}