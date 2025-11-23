using Castle.DynamicProxy;
using System.Threading.Tasks;

namespace Abp.Dependency
{
    /// <summary>
    /// Base class for ABP interceptors that support both synchronous and asynchronous interception.
    /// Uses ABP's own IAsyncInterceptor interface which is compatible with Autofac.Extras.DynamicProxy.
    /// This class provides a consistent interception mechanism that works seamlessly with Autofac's
    /// dependency injection container.
    /// </summary>
    /// <remarks>
    /// This implementation uses ABP's own IAsyncInterceptor interface instead of Castle.Core.AsyncInterceptor.
    /// The public API remains identical to maintain backward compatibility with existing interceptor implementations.
    /// 
    /// To use this interceptor:
    /// 1. Inherit from AbpInterceptorBase
    /// 2. Implement the abstract methods (InterceptSynchronous, InternalInterceptAsynchronous)
    /// 3. Register the interceptor and apply it to services using Autofac's EnableInterfaceInterceptors() or EnableClassInterceptors()
    /// </remarks>
    public abstract class AbpInterceptorBase : IAsyncInterceptor
    {
        /// <summary>
        /// Intercepts an asynchronous method without a return value.
        /// This method is called by the interception infrastructure when an async void method is invoked.
        /// </summary>
        /// <param name="invocation">The method invocation information.</param>
        public virtual void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        /// <summary>
        /// Intercepts an asynchronous method with a return value.
        /// This method is called by the interception infrastructure when an async method with a return value is invoked.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value.</typeparam>
        /// <param name="invocation">The method invocation information.</param>
        public virtual void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        /// <summary>
        /// Intercepts a synchronous method.
        /// Implement this method to provide custom interception logic for synchronous method calls.
        /// </summary>
        /// <param name="invocation">The method invocation information.</param>
        public abstract void InterceptSynchronous(IInvocation invocation);

        /// <summary>
        /// Internal implementation for asynchronous interception without return value.
        /// Implement this method to provide custom interception logic for async void methods.
        /// </summary>
        /// <param name="invocation">The method invocation information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected abstract Task InternalInterceptAsynchronous(IInvocation invocation);

        /// <summary>
        /// Internal implementation for asynchronous interception with return value.
        /// Implement this method to provide custom interception logic for async methods with return values.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value.</typeparam>
        /// <param name="invocation">The method invocation information.</param>
        /// <returns>A task representing the asynchronous operation with a result.</returns>
        protected abstract Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation);

        /// <summary>
        /// Implementation of IInterceptor.Intercept for Castle.DynamicProxy compatibility.
        /// This method is called by Castle.DynamicProxy and delegates to AsyncDeterminationInterceptor logic.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void IInterceptor.Intercept(IInvocation invocation)
        {
            // Delegate to AsyncDeterminationInterceptor logic
            var determinator = new AsyncDeterminationInterceptor(this);
            determinator.Intercept(invocation);
        }
    }
}
