using System;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;

namespace AbpAspNetCoreDemo;

public class TestAssemblyScanning
{
    public static void PrintApplicationServices()
    {
        var assembly = typeof(AbpAspNetCoreDemoModule).Assembly;
        Console.WriteLine($"Assembly: {assembly.FullName}");
        Console.WriteLine($"Location: {assembly.Location}");
        Console.WriteLine();

        var applicationServices = assembly.GetTypes()
            .Where(t => typeof(IApplicationService).IsAssignableFrom(t) &&
                       t.IsPublic &&
                       !t.IsAbstract &&
                       !t.IsGenericType)
            .ToList();

        Console.WriteLine($"Found {applicationServices.Count} ApplicationService classes:");
        foreach (var service in applicationServices)
        {
            Console.WriteLine($"  - {service.FullName}");
        }

        Console.WriteLine();
    }
}
