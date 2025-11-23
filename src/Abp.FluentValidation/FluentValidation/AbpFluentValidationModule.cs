using System;
using Abp.Dependency;
using Abp.FluentValidation.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using FluentValidation;

namespace Abp.FluentValidation {
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpFluentValidationModule : AbpModule {
        public override void ConfigureServices() {
            // Register FluentValidation configuration service
            IocManager.Register<IAbpFluentValidationConfiguration, AbpFluentValidationConfiguration>();

            // Register FluentValidation language manager
            IocManager.Register<AbpFluentValidationLanguageManager, AbpFluentValidationLanguageManager>();

            // Register validator factory
            IocManager.Register<IValidatorFactory, AbpFluentValidationValidatorFactory>(DependencyLifeStyle.Transient);

            // Add conventional registrar for validators
            IocManager.AddConventionalRegistrar(new FluentValidationValidatorRegistrar());
            IocManager.RegisterAssemblyByConvention(typeof(AbpFluentValidationModule).GetAssembly());

            Configuration.Validation.Validators.Add<FluentValidationMethodParameterValidator>();

        }

        public override void Initialize() {
            ValidatorOptions.Global.LanguageManager = IocManager.Resolve<AbpFluentValidationLanguageManager>();

        }
    }
}