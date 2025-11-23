using Abp.AutoMapper;
using Abp.Localization;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Json;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AbpAspNetCoreDemo.Core;

[DependsOn(typeof(AbpAutoMapperModule))]
public class AbpAspNetCoreDemoCoreModule : AbpModule
{
    public override void ConfigureServices()
    {
        // Register assembly by convention
        IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetCoreDemoCoreModule).GetAssembly());

        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        // Configure localization
        Configuration.Localization.Languages.Add(new LanguageInfo("en", "English", isDefault: true));
        Configuration.Localization.Languages.Add(new LanguageInfo("zh-cn", "简体中文"));
        Configuration.Localization.Languages.Add(new LanguageInfo("tr", "Türkçe"));

        Configuration.Localization.Sources.Add(
            new DictionaryBasedLocalizationSource("demo",
                new JsonEmbeddedFileLocalizationDictionaryProvider(
                    typeof(AbpAspNetCoreDemoCoreModule).GetAssembly(),
                    "AbpAspNetCoreDemo.Core.Localization.SourceFiles"
                )
            )
        );



    }

    public override void Initialize()
    {

    }
}