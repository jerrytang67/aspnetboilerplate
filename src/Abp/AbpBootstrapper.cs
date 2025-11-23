using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Dependency.Installers;
using Abp.Domain.Uow;
using Abp.EntityHistory;
using Abp.Modules;
using Abp.Runtime.Validation.Interception;
using Autofac;
using JetBrains.Annotations;

namespace Abp
{
    /// <summary>
    /// This is the main class that is responsible to start entire ABP system.
    /// Prepares dependency injection and registers core components needed for startup.
    /// It must be instantiated and initialized (see <see cref="Initialize"/>) first in an application.
    /// </summary>
    public class AbpBootstrapper : IDisposable
    {
        /// <summary>
        /// Get the startup module of the application which depends on other used modules.
        /// </summary>
        public Type StartupModule { get; }

        /// <summary>
        /// Gets IIocManager object used by this class.
        /// </summary>
        public IIocManager IocManager { get; }

        /// <summary>
        /// Is this object disposed before?
        /// </summary>
        protected bool IsDisposed;

        private AbpModuleManager _moduleManager;
        private ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="AbpBootstrapper"/> instance.
        /// </summary>
        /// <param name="startupModule">Startup module of the application which depends on other used modules. Should be derived from <see cref="AbpModule"/>.</param>
        /// <param name="optionsAction">An action to set options</param>
        private AbpBootstrapper(
            [NotNull] Type startupModule,
            [CanBeNull] Action<AbpBootstrapperOptions> optionsAction = null)
        {
            Check.NotNull(startupModule, nameof(startupModule));

            var options = new AbpBootstrapperOptions();
            optionsAction?.Invoke(options);

            if (!typeof(AbpModule).GetTypeInfo().IsAssignableFrom(startupModule))
            {
                throw new ArgumentException($"{nameof(startupModule)} should be derived from {nameof(AbpModule)}.");
            }

            StartupModule = startupModule;

            IocManager = options.IocManager;

            _logger = null;

            AddInterceptorRegistrars(options.InterceptorOptions);
        }

        /// <summary>
        /// Creates a new <see cref="AbpBootstrapper"/> instance.
        /// </summary>
        /// <typeparam name="TStartupModule">Startup module of the application which depends on other used modules. Should be derived from <see cref="AbpModule"/>.</typeparam>
        /// <param name="optionsAction">An action to set options</param>
        public static AbpBootstrapper Create<TStartupModule>(
            [CanBeNull] Action<AbpBootstrapperOptions> optionsAction = null)
            where TStartupModule : AbpModule
        {
            return new AbpBootstrapper(typeof(TStartupModule), optionsAction);
        }

        /// <summary>
        /// Creates a new <see cref="AbpBootstrapper"/> instance.
        /// </summary>
        /// <param name="startupModule">Startup module of the application which depends on other used modules. Should be derived from <see cref="AbpModule"/>.</param>
        /// <param name="optionsAction">An action to set options</param>
        public static AbpBootstrapper Create(
            [NotNull] Type startupModule, 
            [CanBeNull] Action<AbpBootstrapperOptions> optionsAction = null)
        {
            return new AbpBootstrapper(startupModule, optionsAction);
        }

        private void AddInterceptorRegistrars(
            AbpBootstrapperInterceptorOptions options)
        {
            // Note: Interceptors are now registered automatically via BasicConventionalRegistrar
            // using Autofac.Extras.DynamicProxy's EnableInterfaceInterceptors/EnableClassInterceptors
            // The registrars below are kept for backwards compatibility but don't do anything

            if (!options.DisableValidationInterceptor)
            {
                // Validation interceptor is automatically applied via BasicConventionalRegistrar
                ValidationInterceptorRegistrar.Initialize(IocManager);
            }

            if (!options.DisableAuditingInterceptor)
            {
                // Auditing interceptor is automatically applied via BasicConventionalRegistrar
                AuditingInterceptorRegistrar.Initialize(IocManager);
            }

            if (!options.DisableEntityHistoryInterceptor)
            {
                // EntityHistory interceptor is automatically applied via BasicConventionalRegistrar
                EntityHistoryInterceptorRegistrar.Initialize(IocManager);
            }

            if (!options.DisableUnitOfWorkInterceptor)
            {
                // UnitOfWork interceptor is automatically applied via BasicConventionalRegistrar
                UnitOfWorkRegistrar.Initialize(IocManager);
            }

            if (!options.DisableAuthorizationInterceptor)
            {
                // Authorization interceptor is automatically applied via BasicConventionalRegistrar
                AuthorizationInterceptorRegistrar.Initialize(IocManager);
            }
        }

        /// <summary>
        /// Initializes the ABP system.
        /// This method should be called after the container is built.
        /// </summary>
        public virtual void Initialize()
        {
            ResolveLogger();

            try
            {
                // Verify container is built before proceeding
                var iocMgr = (IocManager)IocManager;
                if (!iocMgr.IsContainerBuilt)
                {
                    throw new AbpInitializationException(
                        "Container must be built before calling Initialize(). " +
                        "Ensure that the container has been built (e.g., via AddAbp() in ASP.NET Core) " +
                        "before calling Initialize().");
                }

                IocManager.Resolve<AbpStartupConfiguration>().Initialize();

                // Resolve module manager from container and start modules
                _moduleManager = IocManager.Resolve<AbpModuleManager>();
                _moduleManager.StartModules();
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "ABP initialization failed");
                throw;
            }
        }

        private void ResolveLogger()
        {
            if (IocManager.IsRegistered<ILoggerFactory>())
            {
                var factory = IocManager.Resolve<ILoggerFactory>();
                _logger = factory.CreateLogger(typeof(AbpBootstrapper).FullName);
            }
        }

        private void RegisterBootstrapper()
        {
            if (!IocManager.IsRegistered<AbpBootstrapper>())
            {
                ((IocManager)IocManager).Builder.RegisterInstance(this).As<AbpBootstrapper>();
            }
        }

        /// <summary>
        /// Disposes the ABP system.
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            _moduleManager?.ShutdownModules();
        }
    }
}
