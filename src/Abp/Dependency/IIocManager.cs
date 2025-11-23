using System;
using Autofac;

namespace Abp.Dependency
{
    /// <summary>
    /// This interface is used to directly perform dependency injection tasks.
    /// </summary>
    public interface IIocManager : IIocRegistrar, IIocResolver, IDisposable
    {
        /// <summary>
        /// Reference to the Autofac Container.
        /// </summary>
        IContainer IocContainer { get; }

        /// <summary>
        /// Gets a value indicating whether the container has been built.
        /// Once built, no further service registrations are allowed.
        /// </summary>
        bool IsContainerBuilt { get; }

        /// <summary>
        /// Builds the container.
        /// This method can only be called once. After the container is built, no further service registrations are allowed.
        /// </summary>
        /// <exception cref="AbpException">Thrown if the container is already built</exception>
        void BuildContainer();

        /// <summary>
        /// Checks whether given type is registered before.
        /// </summary>
        /// <param name="type">Type to check</param>
        new bool IsRegistered(Type type);

        /// <summary>
        /// Checks whether given type is registered before.
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        new bool IsRegistered<T>();
    }
}