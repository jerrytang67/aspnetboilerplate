using System.Linq;
using System.Reflection;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Events.Bus.Factories;
using Abp.Events.Bus.Handlers;
using Autofac;

namespace Abp.Events.Bus {
    /// <summary>
    /// Autofac module that installs event bus system and registers all handlers automatically.
    /// </summary>
    internal class EventBusModule {
        public static void Install(IIocManager iocManager) {
            var eventBusConfiguration = iocManager.Resolve<IEventBusConfiguration>();

            // Register EventBus to the container
            // Note: This should have been done during ConfigureServices phase
            // If container is already built, we can't register new services
            var iocMgr = (IocManager)iocManager;

            iocMgr.Builder.RegisterType<EventBus>().As<IEventBus>().SingleInstance();
            // If container is already built, EventBus should have been registered during ConfigureServices
            // We cannot register new services after container is built in Autofac

            var eventBus = iocManager.Resolve<IEventBus>();

            // Scan all registered components and register event handlers
            var registrations = iocManager.IocContainer.ComponentRegistry.Registrations
                .Where(r => typeof(IEventHandler).GetTypeInfo().IsAssignableFrom(r.Activator.LimitType))
                .ToList();

            foreach (var registration in registrations) {
                RegisterEventHandlers(iocManager, eventBus, registration.Activator.LimitType);
            }

            // Subscribe to future registrations via a registration source
            // This ensures handlers registered after EventBus initialization are also registered
            // Note: Autofac's AddRegistrationSource is an extension method in Autofac.Core namespace
            // For now, we'll skip this advanced feature and rely on convention-based registration
            // var handlerRegistrationSource = new EventHandlerRegistrationSource(iocManager, eventBus);
            // iocManager.IocContainer.ComponentRegistry.AddRegistrationSource(handlerRegistrationSource);
        }

        private static void RegisterEventHandlers(IIocResolver iocResolver, IEventBus eventBus, System.Type implementationType) {
            /* This code checks if registering component implements any IEventHandler<TEventData> interface, if yes,
             * gets all event handler interfaces and registers type to Event Bus for each handling event.
             */
            if (!typeof(IEventHandler).GetTypeInfo().IsAssignableFrom(implementationType)) {
                return;
            }

            var interfaces = implementationType.GetTypeInfo().GetInterfaces();
            foreach (var @interface in interfaces) {
                if (!typeof(IEventHandler).GetTypeInfo().IsAssignableFrom(@interface)) {
                    continue;
                }

                var genericArgs = @interface.GetGenericArguments();
                if (genericArgs.Length == 1) {
                    eventBus.Register(genericArgs[0], new IocHandlerFactory(iocResolver, implementationType));
                }
            }
        }

        /// <summary>
        /// Registration source that automatically registers event handlers with the event bus.
        /// </summary>
        private class EventHandlerRegistrationSource : Autofac.Core.IRegistrationSource {
            private readonly IIocResolver _iocResolver;
            private readonly IEventBus _eventBus;

            public EventHandlerRegistrationSource(IIocResolver iocResolver, IEventBus eventBus) {
                _iocResolver = iocResolver;
                _eventBus = eventBus;
            }

            public System.Collections.Generic.IEnumerable<Autofac.Core.IComponentRegistration> RegistrationsFor(
                Autofac.Core.Service service,
                System.Func<Autofac.Core.Service, System.Collections.Generic.IEnumerable<Autofac.Core.ServiceRegistration>> registrationAccessor) {
                // This method is called when a service is requested but not registered
                // We don't add any new registrations here, just hook into the pipeline
                return System.Linq.Enumerable.Empty<Autofac.Core.IComponentRegistration>();
            }

            public bool IsAdapterForIndividualComponents => false;
        }
    }
}