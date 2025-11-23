using Abp.Dependency;
using Autofac;
using Autofac.Extras.DynamicProxy;
using FluentValidation;

namespace Abp.FluentValidation
{
    public class FluentValidationValidatorRegistrar : IConventionalDependencyRegistrar
    {
        public void RegisterAssembly(IConventionalRegistrationContext context)
        {
            var iocManager = (IocManager)context.IocManager;
            var builder = iocManager.IocContainer != null ? new ContainerBuilder() : iocManager.Builder;

            // Register all validators from the assembly
            builder.RegisterAssemblyTypes(context.Assembly)
                .AsClosedTypesOf(typeof(IValidator<>))
                .InstancePerDependency();
        }
    }
}
