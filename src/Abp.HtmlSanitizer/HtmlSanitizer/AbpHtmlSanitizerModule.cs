using System;
using System.Reflection;
using Abp.Dependency;
using Abp.HtmlSanitizer.ActionFilter;
using Abp.HtmlSanitizer.Configuration;
using Abp.Modules;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Abp.HtmlSanitizer;

[DependsOn(typeof(AbpKernelModule))]
public class AbpHtmlSanitizerModule : AbpModule
{
    public override void ConfigureServices()
    {



        // Register HTML sanitizer configuration
        IocManager.Register<IAsyncActionFilter, AbpHtmlSanitizerActionFilter>(DependencyLifeStyle.Singleton);
        IocManager.Register<IHtmlSanitizerConfiguration, HtmlSanitizerConfiguration>();

        // Register HTML sanitizer as transient (new instance per resolution)
        IocManager.Register<IHtmlSanitizer, Ganss.Xss.HtmlSanitizer>(DependencyLifeStyle.Transient);

        // Register assembly by convention
        IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
    }


    public override void Initialize()
    {
        // Module initialization (no service registrations here)
    }
}