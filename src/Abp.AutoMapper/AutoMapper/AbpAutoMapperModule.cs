using System;
using System.Reflection;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Localization;
using Abp.Modules;
using Abp.Reflection;
using Autofac;
using AutoMapper;

namespace Abp.AutoMapper;

[DependsOn(typeof(AbpKernelModule))]
public class AbpAutoMapperModule : AbpModule
{
    private ITypeFinder _typeFinder;

    public AbpAutoMapperModule()
    {
        // TypeFinder will be resolved from IocManager when needed
    }

    public AbpAutoMapperModule(ITypeFinder typeFinder)
    {
        _typeFinder = typeFinder;
    }

    public override void ConfigureServices()
    {
        // Register AutoMapper configuration service
        IocManager.Register<IAbpAutoMapperConfiguration, AbpAutoMapperConfiguration>();

        // IMPORTANT: Store the configuration instance in the Configuration dictionary
        var autoMapperConfig = new AbpAutoMapperConfiguration();
        Configuration.Set(typeof(IAbpAutoMapperConfiguration).FullName, autoMapperConfig);

        // Register AutoMapper object mapper
        IocManager.Register<ObjectMapping.IObjectMapper, AutoMapperObjectMapper>();

        // Register AutoMapper configuration and mapper using factory pattern
        // The factory will be invoked lazily when IMapper is first resolved (in Initialize or later)
        var iocMgr = (IocManager)IocManager;

        iocMgr.Builder.Register(ctx =>
        {
            // Resolve startup configuration to get AutoMapper configurators
            // This will be called after Initialize() has added the configurators
            var startupConfig = ctx.Resolve<IAbpStartupConfiguration>();
            var autoMapperCfg = startupConfig.Modules.AbpAutoMapper();

            Action<IMapperConfigurationExpression> configurer = configuration =>
            {
                // Resolve TypeFinder for scanning types
                var typeFinder = ctx.Resolve<ITypeFinder>();
                var types = typeFinder.Find(type =>
                {
                    var typeInfo = type.GetTypeInfo();
                    return typeInfo.IsDefined(typeof(AutoMapAttribute)) ||
                           typeInfo.IsDefined(typeof(AutoMapFromAttribute)) ||
                           typeInfo.IsDefined(typeof(AutoMapToAttribute));
                });

                foreach (var type in types)
                {
                    configuration.CreateAutoAttributeMaps(type);
                }

                // Apply all configurators
                foreach (var configurator in autoMapperCfg.Configurators)
                {
                    configurator(configuration);
                }
            };

            return new MapperConfiguration(configurer);
        })
        .As<IConfigurationProvider>()
        .SingleInstance();

        iocMgr.Builder.Register(ctx =>
        {
            var config = ctx.Resolve<IConfigurationProvider>() as MapperConfiguration;
            return config.CreateMapper();
        })
        .As<IMapper>()
        .SingleInstance();

        // Add core mappings configurator (requires Configuration to be initialized)


        // Replace default object mapper with AutoMapper implementation
        Configuration.ReplaceService<ObjectMapping.IObjectMapper, AutoMapperObjectMapper>();
    }

    public override void Initialize()
    {

        Configuration.Modules.AbpAutoMapper().Configurators.Add(CreateCoreMappings);
        // Resolve and set the static mapper for backward compatibility
        // This will trigger the lazy factory registration above
        var mapper = IocManager.Resolve<IMapper>();
#pragma warning disable CS0618 // Type or member is obsolete, this line will be removed once AutoMapper is updated
        AbpEmulateAutoMapper.Mapper = mapper;
#pragma warning restore CS0618 // Type or member is obsolete, this line will be removed once AutoMapper is updated
    }

    private void CreateCoreMappings(IMapperConfigurationExpression configuration)
    {
        var localizationContext = IocManager.Resolve<ILocalizationContext>();

        configuration.CreateMap<ILocalizableString, string>().ConvertUsing(ls => ls == null ? null : ls.Localize(localizationContext));
        configuration.CreateMap<LocalizableString, string>().ConvertUsing(ls => ls == null ? null : localizationContext.LocalizationManager.GetString(ls));
    }
}