using System;
using System.IO;
using System.Linq.Expressions;
using Abp.Application.Features;
using Abp.Application.Navigation;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.CachedUniqueKeys;
using Abp.Collections.Extensions;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.DynamicEntityProperties;
using Abp.EntityHistory;
using Abp.Events.Bus;
using Abp.Localization;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.MultiTenancy;
using Abp.Net.Mail;
using Abp.Notifications;
using Abp.RealTime;
using Abp.Reflection.Extensions;
using Abp.Runtime;
using Abp.Runtime.Caching;
using Abp.Runtime.Remoting;
using Abp.Runtime.Validation.Interception;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using Abp.Webhooks;
using Autofac;

namespace Abp {
    /// <summary>
    /// Kernel (core) module of the ABP system.
    /// No need to depend on this, it's automatically the first module always.
    /// </summary>
    public sealed class AbpKernelModule : AbpModule {
        public override void ConfigureServices() {
            // Register conventional registrars
            IocManager.AddConventionalRegistrar(new BasicConventionalRegistrar());

            // Register core services
            IocManager.Register<ISettingDefinitionManager, SettingDefinitionManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<LocalizationSettingProvider>(DependencyLifeStyle.Transient);
            IocManager.Register<EmailSettingProvider>(DependencyLifeStyle.Transient);
            IocManager.Register<NotificationSettingProvider>(DependencyLifeStyle.Transient);
            IocManager.Register<TimingSettingProvider>(DependencyLifeStyle.Transient);
            IocManager.Register<IFeatureManager, Abp.Application.Features.FeatureManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<IPermissionManager, Abp.Authorization.PermissionManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<INotificationDefinitionManager, NotificationDefinitionManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<INavigationManager, NavigationManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<IWebhookDefinitionManager, WebhookDefinitionManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<IDynamicEntityPropertyDefinitionManager, DynamicEntityPropertyDefinitionManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<IBackgroundJobManager, BackgroundJobManager>(DependencyLifeStyle.Singleton);
            IocManager.Register<IRunnable, AbpAsyncTimer>(DependencyLifeStyle.Transient);


            IocManager.Register<IScopedIocResolver, ScopedIocResolver>(DependencyLifeStyle.Transient);


            IocManager.Register(typeof(IAmbientScopeProvider<>), typeof(DataContextAmbientScopeProvider<>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(EventTriggerAsyncBackgroundJob<>), DependencyLifeStyle.Transient);

            // Register EventBus early (before container is built)
            RegisterEventBus();

            // Register assembly by convention
            IocManager.RegisterAssemblyByConvention(typeof(AbpKernelModule).GetAssembly(),
                new ConventionalRegistrationConfig {
                    InstallInstallers = false
                });

            // Register interceptors
            RegisterInterceptors();

            // Register default implementations for optional services
            RegisterDefaultImplementations();

            // Configure UnitOfWork filters and audit fields BEFORE container is built
            // This ensures filters are registered before any UnitOfWork instances are created
            AddUnitOfWorkFilters();
            AddUnitOfWorkAuditFieldConfiguration();


            // Configure module settings BEFORE initializing managers
            // The managers read these configurations during their Initialize() calls
            AddAuditingSelectors();
            AddLocalizationSources();
            AddSettingProviders();
            ConfigureCaches();
            AddIgnoredTypes();
            AddMethodParameterValidators();

            RegisterMissingComponents();
        }


        private void RegisterEventBus() {
            // Get event bus configuration
            // Note: Configuration is already initialized in EarlyInitialize()
            // var eventBusConfiguration = Configuration.Get<IEventBusConfiguration>();
            //
            var iocMgr = (IocManager)IocManager;
            // if (eventBusConfiguration.UseDefaultEventBus)
            // {
            // iocMgr.Builder.RegisterInstance(EventBus.Default).As<IEventBus>().SingleInstance();
            // }
            // else
            // {
            iocMgr.Builder.RegisterType<EventBus>().As<IEventBus>().SingleInstance();
            // }
        }

        public override void PostConfigureServices() {
            // Execute service replacement actions AFTER all modules have registered their services
            // but BEFORE the container is built

            // Debug: Check if container is already built
            var iocMgr = (IocManager)IocManager;
            if (iocMgr.IsContainerBuilt) {
                throw new AbpException(
                    "Container is already built in PostConfigureServices! " +
                    "This should not happen. Container should only be built after all ConfigureServices methods complete.");
            }

            foreach (var replaceAction in ((AbpStartupConfiguration)Configuration).ServiceReplaceActions.Values) {
                replaceAction();
            }
        }

        public override void Initialize() {
            IocManager.Resolve<SettingDefinitionManager>().Initialize();
            IocManager.Resolve<FeatureManager>().Initialize();
            IocManager.Resolve<PermissionManager>().Initialize();
            IocManager.Resolve<LocalizationManager>().Initialize();
            IocManager.Resolve<NotificationDefinitionManager>().Initialize();
            IocManager.Resolve<NavigationManager>().Initialize();
            IocManager.Resolve<WebhookDefinitionManager>().Initialize();
            IocManager.Resolve<DynamicEntityPropertyDefinitionManager>().Initialize();

            if (Configuration.BackgroundJobs.IsJobExecutionEnabled) {
                var workerManager = IocManager.Resolve<IBackgroundWorkerManager>();
                workerManager.Start();
                workerManager.Add(IocManager.Resolve<IBackgroundJobManager>());
            }
        }

        private void RegisterInterceptors() {
            // Register interceptor wrapper classes for Autofac interception
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<UnitOfWorkInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<AuditingInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<AuthorizationInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<ValidationInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<EntityHistoryInterceptor>), DependencyLifeStyle.Transient);
        }

        private void RegisterDefaultImplementations() {
            // Register default implementations for optional services
            // These MUST be registered before the container is built
            IocManager.RegisterIfNot<IUnitOfWork, NullUnitOfWork>(DependencyLifeStyle.Transient);
            IocManager.RegisterIfNot<IAuditingStore, SimpleLogAuditingStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IPermissionChecker, NullPermissionChecker>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<INotificationStore, NullNotificationStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IUnitOfWorkFilterExecuter, NullUnitOfWorkFilterExecuter>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IClientInfoProvider, NullClientInfoProvider>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<ITenantStore, NullTenantStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<ITenantResolverCache, NullTenantResolverCache>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IEntityHistoryStore, NullEntityHistoryStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<ICachedUniqueKeyPerUser, CachedUniqueKeyPerUser>(DependencyLifeStyle.Transient);

            // Register InMemoryBackgroundJobStore as default
            // This can be overridden by modules if needed
            IocManager.RegisterIfNot<IBackgroundJobStore, InMemoryBackgroundJobStore>(DependencyLifeStyle.Singleton);
        }


        public override void Shutdown() {
            if (Configuration.BackgroundJobs.IsJobExecutionEnabled) {
                IocManager.Resolve<IBackgroundWorkerManager>().StopAndWaitToStop();
            }
        }

        private void AddUnitOfWorkFilters() {
            Configuration.UnitOfWork.RegisterFilter(AbpDataFilters.SoftDelete, true);
            Configuration.UnitOfWork.RegisterFilter(AbpDataFilters.MustHaveTenant, true);
            Configuration.UnitOfWork.RegisterFilter(AbpDataFilters.MayHaveTenant, true);
        }

        private void AddUnitOfWorkAuditFieldConfiguration() {
            Configuration.UnitOfWork.RegisterAuditFieldConfiguration(AbpAuditFields.CreatorUserId, true);
            Configuration.UnitOfWork.RegisterAuditFieldConfiguration(AbpAuditFields.LastModifierUserId, true);
            Configuration.UnitOfWork.RegisterAuditFieldConfiguration(AbpAuditFields.LastModificationTime, true);
            Configuration.UnitOfWork.RegisterAuditFieldConfiguration(AbpAuditFields.DeleterUserId, true);
            Configuration.UnitOfWork.RegisterAuditFieldConfiguration(AbpAuditFields.DeletionTime, true);
        }

        private void AddSettingProviders() {
            Configuration.Settings.Providers.Add<LocalizationSettingProvider>();
            Configuration.Settings.Providers.Add<EmailSettingProvider>();
            Configuration.Settings.Providers.Add<NotificationSettingProvider>();
            Configuration.Settings.Providers.Add<TimingSettingProvider>();
        }

        private void AddAuditingSelectors() {
            Configuration.Auditing.Selectors.Add(
                new NamedTypeSelector(
                    "Abp.ApplicationServices",
                    type => typeof(IApplicationService).IsAssignableFrom(type)
                )
            );
        }

        private void AddLocalizationSources() {
            Configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    AbpConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AbpKernelModule).GetAssembly(), "Abp.Localization.Sources.AbpXmlSource"
                    )));
        }

        private void ConfigureCaches() {
            Configuration.Caching.Configure(AbpCacheNames.ApplicationSettings, cache => { cache.DefaultSlidingExpireTime = TimeSpan.FromHours(8); });

            Configuration.Caching.Configure(AbpCacheNames.TenantSettings, cache => { cache.DefaultSlidingExpireTime = TimeSpan.FromMinutes(60); });

            Configuration.Caching.Configure(AbpCacheNames.UserSettings, cache => { cache.DefaultSlidingExpireTime = TimeSpan.FromMinutes(20); });
        }

        private void AddIgnoredTypes() {
            var commonIgnoredTypes = new[] {
                typeof(Stream),
                typeof(Expression)
            };

            foreach (var ignoredType in commonIgnoredTypes) {
                Configuration.Auditing.IgnoredTypes.AddIfNotContains(ignoredType);
                Configuration.Validation.IgnoredTypes.AddIfNotContains(ignoredType);
            }

            var validationIgnoredTypes = new[] { typeof(Type) };
            foreach (var ignoredType in validationIgnoredTypes) {
                Configuration.Validation.IgnoredTypes.AddIfNotContains(ignoredType);
            }
        }

        private void AddMethodParameterValidators() {
            Configuration.Validation.Validators.Add<DataAnnotationsValidator>();
            Configuration.Validation.Validators.Add<ValidatableObjectValidator>();
            Configuration.Validation.Validators.Add<CustomValidator>();
        }

        private void RegisterMissingComponents() {
            // Only register components that need to be registered after container is built
            // Most default implementations are now registered in ConfigureServices via RegisterDefaultImplementations()

            if (!IocManager.IsRegistered<IGuidGenerator>()) {
                var iocMgr = (IocManager)IocManager;
                iocMgr.Builder.RegisterInstance(SequentialGuidGenerator.Instance)
                    .As<IGuidGenerator>()
                    .As<SequentialGuidGenerator>();
            }
        }
    }
}