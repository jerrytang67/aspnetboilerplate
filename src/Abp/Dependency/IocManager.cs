using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace Abp.Dependency
{
    /// <summary>
    /// This class is used to directly perform dependency injection tasks.
    /// </summary>
    public class IocManager : IIocManager
    {
        /// <summary>
        /// The Singleton instance.
        /// </summary>
        public static IocManager Instance { get; private set; }

        /// <summary>
        /// Reference to the Autofac Container.
        /// </summary>
        public IContainer IocContainer { get; private set; }

        /// <summary>
        /// Container builder used for registrations before the container is built.
        /// </summary>
        private ContainerBuilder _builder;

        /// <summary>
        /// Gets the container builder for pre-build registrations.
        /// </summary>
        public ContainerBuilder Builder => _builder;

        /// <summary>
        /// List of all registered conventional registrars.
        /// </summary>
        private readonly List<IConventionalDependencyRegistrar> _conventionalRegistrars;

        /// <summary>
        /// Set of types that have been registered.
        /// Used to check registration status before container is built.
        /// </summary>
        private readonly HashSet<Type> _registeredTypes;

        /// <summary>
        /// Indicates whether the container has been built.
        /// </summary>
        private bool _isContainerBuilt;

        /// <summary>
        /// Gets a value indicating whether the container has been built.
        /// Once built, no further service registrations are allowed.
        /// </summary>
        public bool IsContainerBuilt => _isContainerBuilt;

        static IocManager()
        {
            Instance = new IocManager();
        }

        /// <summary>
        /// Creates a new <see cref="IocManager"/> object.
        /// Normally, you don't directly instantiate an <see cref="IocManager"/>.
        /// This may be useful for test purposes.
        /// </summary>
        public IocManager()
        {
            _builder = new ContainerBuilder();
            _conventionalRegistrars = new List<IConventionalDependencyRegistrar>();
            _registeredTypes = new HashSet<Type>();
            _isContainerBuilt = false;

            //Register self!
            _builder.RegisterInstance(this)
                .As<IocManager>()
                .As<IIocManager>()
                .As<IIocRegistrar>()
                .As<IIocResolver>()
                .SingleInstance();

            // Track these types as registered
            _registeredTypes.Add(typeof(IocManager));
            _registeredTypes.Add(typeof(IIocManager));
            _registeredTypes.Add(typeof(IIocRegistrar));
            _registeredTypes.Add(typeof(IIocResolver));
        }

        /// <summary>
        /// Builds the container.
        /// This method can only be called once. After the container is built, no further service registrations are allowed.
        /// </summary>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        public void BuildContainer()
        {
            if (_isContainerBuilt)
            {
                throw new AbpException(
                    "Container is already built and cannot be rebuilt. " +
                    "BuildContainer() can only be called once.");
            }

            // Log the call stack when container is built
            var stackTrace = new System.Diagnostics.StackTrace(true);
            Console.WriteLine("=== CONTAINER BUILDING ===");
            Console.WriteLine("Container is being built at:");
            Console.WriteLine(stackTrace.ToString());
            Console.WriteLine("=========================");

            IocContainer = _builder.Build();
            _isContainerBuilt = true;
        }

        protected virtual IContainer CreateContainer()
        {
            BuildContainer();
            return IocContainer;
        }

        /// <summary>
        /// Adds a dependency registrar for conventional registration.
        /// </summary>
        /// <param name="registrar">dependency registrar</param>
        public void AddConventionalRegistrar(IConventionalDependencyRegistrar registrar)
        {
            _conventionalRegistrars.Add(registrar);
        }

        /// <summary>
        /// Registers types of given assembly by all conventional registrars. See <see cref="AddConventionalRegistrar"/> method.
        /// </summary>
        /// <param name="assembly">Assembly to register</param>
        public void RegisterAssemblyByConvention(Assembly assembly)
        {
            RegisterAssemblyByConvention(assembly, new ConventionalRegistrationConfig());
        }

        /// <summary>
        /// Registers types of given assembly by all conventional registrars. See <see cref="AddConventionalRegistrar"/> method.
        /// </summary>
        /// <param name="assembly">Assembly to register</param>
        /// <param name="config">Additional configuration</param>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        public void RegisterAssemblyByConvention(Assembly assembly, ConventionalRegistrationConfig config)
        {
            if (_isContainerBuilt)
            {
                throw new AbpException(
                    "Cannot register services after container is built. " +
                    "All service registrations must occur in the ConfigureServices() method " +
                    "before the container is constructed. " +
                    $"Attempted to register assembly: {assembly.FullName}");
            }

            var context = new ConventionalRegistrationContext(assembly, this, config);

            foreach (var registerer in _conventionalRegistrars)
            {
                registerer.RegisterAssembly(context);
            }

            if (config.InstallInstallers)
            {
                // Autofac uses Modules instead of Installers
                // Find and register all modules from the assembly
                var moduleType = typeof(Autofac.Module);
                var modules = assembly.GetTypes()
                    .Where(t => moduleType.IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => (Autofac.Module)Activator.CreateInstance(t))
                    .ToList();

                foreach (var module in modules)
                {
                    _builder.RegisterModule(module);
                }
            }
        }

        /// <summary>
        /// Registers a type as self registration.
        /// </summary>
        /// <typeparam name="TType">Type of the class</typeparam>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        public void Register<TType>(DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton) where TType : class
        {
            if (_isContainerBuilt)
            {
                throw new AbpException(
                    "Cannot register services after container is built. " +
                    "All service registrations must occur in the ConfigureServices() method " +
                    "before the container is constructed. " +
                    $"Attempted to register: {typeof(TType).FullName}");
            }

            var registration = _builder.RegisterType<TType>().AsSelf();
            ApplyLifestyle(registration, lifeStyle);
            _registeredTypes.Add(typeof(TType));
        }

        /// <summary>
        /// Registers a type as self registration.
        /// </summary>
        /// <param name="type">Type of the class</param>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        public void Register(Type type, DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
        {
            if (_isContainerBuilt)
            {
                throw new AbpException(
                    "Cannot register services after container is built. " +
                    "All service registrations must occur in the ConfigureServices() method " +
                    "before the container is constructed. " +
                    $"Attempted to register: {type.FullName}");
            }

            // Handle open generic types
            if (type.IsGenericTypeDefinition)
            {
                var registration = _builder.RegisterGeneric(type).AsSelf();
                ApplyLifestyle(registration, lifeStyle);
            }
            else
            {
                var registration = _builder.RegisterType(type).AsSelf();
                ApplyLifestyle(registration, lifeStyle);
            }
            _registeredTypes.Add(type);
        }

        /// <summary>
        /// Registers a type with it's implementation.
        /// </summary>
        /// <typeparam name="TType">Registering type</typeparam>
        /// <typeparam name="TImpl">The type that implements <typeparamref name="TType"/></typeparam>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        public void Register<TType, TImpl>(DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
            where TType : class
            where TImpl : class, TType
        {
            if (_isContainerBuilt)
            {
                throw new AbpException(
                    "Cannot register services after container is built. " +
                    "All service registrations must occur in the ConfigureServices() method " +
                    "before the container is constructed. " +
                    $"Attempted to register: {typeof(TType).FullName} with implementation {typeof(TImpl).FullName}");
            }

            var registration = _builder.RegisterType<TImpl>().As<TType>().As<TImpl>();
            ApplyLifestyle(registration, lifeStyle);
            _registeredTypes.Add(typeof(TType));
            _registeredTypes.Add(typeof(TImpl));
        }

        /// <summary>
        /// Registers a type with it's implementation.
        /// </summary>
        /// <param name="type">Type of the class</param>
        /// <param name="impl">The type that implements <paramref name="type"/></param>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        public void Register(Type type, Type impl, DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
        {
            if (_isContainerBuilt)
            {
                throw new AbpException(
                    "Cannot register services after container is built. " +
                    "All service registrations must occur in the ConfigureServices() method " +
                    "before the container is constructed. " +
                    $"Attempted to register: {type.FullName} with implementation {impl.FullName}");
            }

            // Handle open generic types
            if (type.IsGenericTypeDefinition && impl.IsGenericTypeDefinition)
            {
                var registration = _builder.RegisterGeneric(impl).As(type);
                ApplyLifestyle(registration, lifeStyle);
            }
            else
            {
                var registration = _builder.RegisterType(impl).As(type).As(impl);
                ApplyLifestyle(registration, lifeStyle);
            }
            _registeredTypes.Add(type);
            _registeredTypes.Add(impl);
        }

        /// <summary>
        /// Checks whether given type is registered before.
        /// </summary>
        /// <param name="type">Type to check</param>
        public bool IsRegistered(Type type)
        {
            // If container is built, check the container
            if (_isContainerBuilt)
            {
                return IocContainer.IsRegistered(type);
            }
            // Otherwise, check the tracking set
            return _registeredTypes.Contains(type);
        }

        /// <summary>
        /// Checks whether given type is registered before.
        /// </summary>
        /// <typeparam name="TType">Type to check</typeparam>
        public bool IsRegistered<TType>()
        {
            // If container is built, check the container
            if (_isContainerBuilt)
            {
                return IocContainer.IsRegistered<TType>();
            }
            // Otherwise, check the tracking set
            return _registeredTypes.Contains(typeof(TType));
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary>
        /// <typeparam name="T">Type of the object to get</typeparam>
        /// <returns>The instance object</returns>
        public T Resolve<T>()
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because Resolve<{typeof(T).Name}>() was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            return IocContainer.Resolve<T>();
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="Release"/>) after usage.
        /// </summary>
        /// <typeparam name="T">Type of the object to cast</typeparam>
        /// <param name="type">Type of the object to resolve</param>
        /// <returns>The object instance</returns>
        public T Resolve<T>(Type type)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because Resolve<{typeof(T).Name}>({type.Name}) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            return (T)IocContainer.Resolve(type);
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary>
        /// <typeparam name="T">Type of the object to get</typeparam>
        /// <param name="argumentsAsAnonymousType">Constructor arguments</param>
        /// <returns>The instance object</returns>
        public T Resolve<T>(object argumentsAsAnonymousType)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because Resolve<{typeof(T).Name}>(args) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            var parameters = CreateNamedParameters(argumentsAsAnonymousType);
            return IocContainer.Resolve<T>(parameters);
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary>
        /// <param name="type">Type of the object to get</param>
        /// <returns>The instance object</returns>
        public object Resolve(Type type)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because Resolve({type.Name}) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            return IocContainer.Resolve(type);
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary>
        /// <param name="type">Type of the object to get</param>
        /// <param name="argumentsAsAnonymousType">Constructor arguments</param>
        /// <returns>The instance object</returns>
        public object Resolve(Type type, object argumentsAsAnonymousType)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because Resolve({type.Name}, args) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            var parameters = CreateNamedParameters(argumentsAsAnonymousType);
            return IocContainer.Resolve(type, parameters);
        }

        ///<inheritdoc/>
        public T[] ResolveAll<T>()
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because ResolveAll<{typeof(T).Name}>() was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            return IocContainer.Resolve<IEnumerable<T>>().ToArray();
        }

        ///<inheritdoc/>
        public T[] ResolveAll<T>(object argumentsAsAnonymousType)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because ResolveAll<{typeof(T).Name}>(args) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            var parameters = CreateNamedParameters(argumentsAsAnonymousType);
            return IocContainer.Resolve<IEnumerable<T>>(parameters).ToArray();
        }

        ///<inheritdoc/>
        public object[] ResolveAll(Type type)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because ResolveAll({type.Name}) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(type);
            return ((System.Collections.IEnumerable)IocContainer.Resolve(enumerableType)).Cast<object>().ToArray();
        }

        ///<inheritdoc/>
        public object[] ResolveAll(Type type, object argumentsAsAnonymousType)
        {
            if (!_isContainerBuilt)
            {
                // Log the call stack when Resolve triggers container building
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine("=== AUTO-BUILDING CONTAINER ===");
                Console.WriteLine($"Container is being auto-built because ResolveAll({type.Name}, args) was called before BuildContainer().");
                Console.WriteLine("Call stack:");
                Console.WriteLine(stackTrace.ToString());
                Console.WriteLine("================================");

                BuildContainer();
            }
            var parameters = CreateNamedParameters(argumentsAsAnonymousType);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(type);
            return ((System.Collections.IEnumerable)IocContainer.Resolve(enumerableType, parameters)).Cast<object>().ToArray();
        }

        /// <summary>
        /// Releases a pre-resolved object. See Resolve methods.
        /// </summary>
        /// <param name="obj">Object to be released</param>
        public void Release(object obj)
        {
            // Autofac manages lifetimes automatically via scopes
            // Explicit release is generally not needed for non-lifetime-scope managed instances
            // However, we keep this method for API compatibility
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            IocContainer?.Dispose();
        }

        private IEnumerable<NamedParameter> CreateNamedParameters(object argumentsAsAnonymousType)
        {
            var props = argumentsAsAnonymousType.GetType().GetProperties();
            return props.Select(p => new NamedParameter(p.Name, p.GetValue(argumentsAsAnonymousType)));
        }

        private void ApplyLifestyle<TLimit, TActivatorData, TRegistrationStyle>(
            IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
            DependencyLifeStyle lifeStyle)
        {
            switch (lifeStyle)
            {
                case DependencyLifeStyle.Transient:
                    registration.InstancePerDependency();
                    break;
                case DependencyLifeStyle.Singleton:
                    registration.SingleInstance();
                    break;
            }
        }


    }
}
