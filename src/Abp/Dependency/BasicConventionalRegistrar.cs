using System;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Uow;
using Abp.EntityHistory;
using Abp.Runtime.Validation.Interception;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy;

namespace Abp.Dependency
{
    /// <summary>
    /// This class is used to register basic dependency implementations such as <see cref="ITransientDependency"/> and <see cref="ISingletonDependency"/>.
    /// </summary>
    public class BasicConventionalRegistrar : IConventionalDependencyRegistrar
    {
        public void RegisterAssembly(IConventionalRegistrationContext context)
        {
            var iocManager = (IocManager)context.IocManager;
            ContainerBuilder builder;

            // If container is already built, create a new builder for updates
            // Otherwise, use the main builder
            if (iocManager.IocContainer != null)
            {
                builder = new ContainerBuilder();
            }
            else
            {
                builder = iocManager.Builder;
            }

            // Get all types from assembly
            var types = context.Assembly.GetTypes()
                .Where(type => !type.GetTypeInfo().IsGenericTypeDefinition && !type.GetTypeInfo().IsAbstract)
                .ToList();

            // Register Transient dependencies
            foreach (var type in types.Where(t => typeof(ITransientDependency).IsAssignableFrom(t)))
            {
                var registration = builder.RegisterType(type)
                    // .AsSelf()
                    .AsImplementedInterfaces()
                    .PropertiesAutowired(new DoNotWirePropertySelector())
                    .InstancePerDependency();

                // Enable interceptors if needed
                ApplyInterceptors(registration, type, context.IocManager);
            }

            // Register Singleton dependencies
            foreach (var type in types.Where(t => typeof(ISingletonDependency).IsAssignableFrom(t)))
            {
                var registration = builder.RegisterType(type)
                    // .AsSelf()
                    .AsImplementedInterfaces()
                    .PropertiesAutowired(new DoNotWirePropertySelector())
                    .SingleInstance();

                // Enable interceptors if needed
                ApplyInterceptors(registration, type, context.IocManager);
            }
        }

        /// <summary>
        /// Property selector that respects DoNotWireAttribute
        /// </summary>
        private class DoNotWirePropertySelector : IPropertySelector
        {
            public bool InjectProperty(PropertyInfo propertyInfo, object instance)
            {
                // Don't wire properties marked with DoNotWireAttribute
                if (propertyInfo.GetCustomAttribute<DoNotWireAttribute>() != null)
                {
                    return false;
                }

                // Don't wire properties on classes marked with DoNotWireAttribute
                if (propertyInfo.DeclaringType?.GetCustomAttribute<DoNotWireAttribute>() != null)
                {
                    return false;
                }

                // Default Autofac behavior: wire public writable properties
                return propertyInfo.CanWrite && propertyInfo.SetMethod.IsPublic;
            }
        }

        private void ApplyInterceptors<TLimit, TActivatorData, TRegistrationStyle>(
            IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
            Type implementationType,
            IIocManager iocManager)
        {
            var interceptorTypes = GetInterceptorTypes(implementationType, iocManager).ToList();

            if (!interceptorTypes.Any())
            {
                return;
            }

            // Enable interface interception (works for most ABP services)
            // Class interception would require specific type constraints
            var typeInfo = implementationType.GetTypeInfo();
            var interfaces = typeInfo.GetInterfaces();

            if (interfaces.Any())
            {
                registration.EnableInterfaceInterceptors();

                // Add interceptors
                foreach (var interceptorType in interceptorTypes)
                {
                    registration.InterceptedBy(interceptorType);
                }
            }
        }

        private System.Collections.Generic.IEnumerable<Type> GetInterceptorTypes(Type implementationType, IIocManager iocManager)
        {
            var interceptors = new System.Collections.Generic.List<Type>();
            var typeInfo = implementationType.GetTypeInfo();

            // UnitOfWork interceptor
            if (ShouldInterceptForUnitOfWork(typeInfo, iocManager))
            {
                interceptors.Add(typeof(AbpAsyncDeterminationInterceptor<UnitOfWorkInterceptor>));
            }

            // Authorization interceptor
            if (ShouldInterceptForAuthorization(typeInfo))
            {
                interceptors.Add(typeof(AbpAsyncDeterminationInterceptor<AuthorizationInterceptor>));
            }

            // Auditing interceptor
            if (ShouldInterceptForAuditing(typeInfo, iocManager))
            {
                interceptors.Add(typeof(AbpAsyncDeterminationInterceptor<AuditingInterceptor>));
            }

            // Validation interceptor
            if (ShouldInterceptForValidation(typeInfo))
            {
                interceptors.Add(typeof(AbpAsyncDeterminationInterceptor<ValidationInterceptor>));
            }

            // EntityHistory interceptor
            if (ShouldInterceptForEntityHistory(typeInfo))
            {
                interceptors.Add(typeof(AbpAsyncDeterminationInterceptor<EntityHistoryInterceptor>));
            }

            return interceptors;
        }

        private bool ShouldInterceptForUnitOfWork(TypeInfo typeInfo, IIocManager iocManager)
        {
            if (UnitOfWorkHelper.HasUnitOfWorkAttribute(typeInfo))
            {
                return true;
            }

            if (typeInfo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(UnitOfWorkHelper.HasUnitOfWorkAttribute))
            {
                return true;
            }

            if (iocManager.IsRegistered<IUnitOfWorkDefaultOptions>())
            {
                var uowOptions = iocManager.Resolve<IUnitOfWorkDefaultOptions>();
                if (uowOptions.IsConventionalUowClass(typeInfo.AsType()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldInterceptForAuthorization(TypeInfo typeInfo)
        {
            if (typeInfo.IsDefined(typeof(AbpAuthorizeAttribute), true))
            {
                return true;
            }

            if (typeInfo.GetMethods().Any(m => m.IsDefined(typeof(AbpAuthorizeAttribute), true)))
            {
                return true;
            }

            return false;
        }

        private bool ShouldInterceptForAuditing(TypeInfo typeInfo, IIocManager iocManager)
        {
            if (typeInfo.IsDefined(typeof(AuditedAttribute), true))
            {
                return true;
            }

            if (typeInfo.GetMethods().Any(m => m.IsDefined(typeof(AuditedAttribute), true)))
            {
                return true;
            }

            if (iocManager.IsRegistered<IAbpAuditingDefaultOptions>())
            {
                var auditingOptions = iocManager.Resolve<IAbpAuditingDefaultOptions>();
                if (auditingOptions.ConventionalAuditingSelectors.Any(selector => selector(typeInfo.AsType())))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldInterceptForValidation(TypeInfo typeInfo)
        {
            // Check if type is an application service (needs validation)
            if (typeof(IApplicationService).IsAssignableFrom(typeInfo.AsType()))
            {
                return true;
            }

            return false;
        }

        private bool ShouldInterceptForEntityHistory(TypeInfo typeInfo)
        {
            if (typeInfo.IsDefined(typeof(UseCaseAttribute), true))
            {
                return true;
            }

            if (typeInfo.GetMethods().Any(m => m.IsDefined(typeof(UseCaseAttribute), true)))
            {
                return true;
            }

            return false;
        }
    }
}
