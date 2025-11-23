using System;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;
using Abp.AspNetCore.Configuration;
using Abp.Collections.Extensions;
using Abp.Dependency;
using Abp.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Abp.AspNetCore.Mvc.Providers;

/// <summary>
/// Used to add application services as controller.
/// </summary>
public class AbpAppServiceControllerFeatureProvider : ControllerFeatureProvider
{
    private readonly IIocResolver _iocResolver;

    public AbpAppServiceControllerFeatureProvider(IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        var type = typeInfo.AsType();

        if (!typeof(IApplicationService).IsAssignableFrom(type) ||
            !typeInfo.IsPublic || typeInfo.IsAbstract || typeInfo.IsGenericType)
        {
            return false;
        }

        var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(typeInfo);

        if (remoteServiceAttr != null && !remoteServiceAttr.IsEnabledFor(type))
        {
            return false;
        }

        try
        {
            Console.WriteLine($"[IsController] Checking type: {type.FullName}");
            var config = _iocResolver.Resolve<AbpAspNetCoreConfiguration>();
            Console.WriteLine($"[IsController] Config resolved: {config != null}");
            Console.WriteLine($"[IsController] ControllerAssemblySettings count: {config?.ControllerAssemblySettings?.Count ?? 0}");
            
            var settings = config.ControllerAssemblySettings.GetSettings(type);
            Console.WriteLine($"[IsController] Settings found for type: {settings?.Count ?? 0}");
            
            var result = settings.Any(setting => setting.TypePredicate(type));
            Console.WriteLine($"[IsController] Result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IsController] Exception: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[IsController] StackTrace: {ex.StackTrace}");
            return false;
        }
    }
}