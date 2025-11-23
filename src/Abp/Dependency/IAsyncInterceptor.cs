using Castle.DynamicProxy;

namespace Abp.Dependency
{
    /// <summary>
    /// Interface for interceptors that support both synchronous and asynchronous method interception.
    /// This is ABP's own implementation compatible with Autofac.Extras.DynamicProxy.
    /// </summary>
    public interface IAsyncInterceptor : IInterceptor
    {
        /// <summary>
        /// Intercepts a synchronous method invocation.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void InterceptSynchronous(IInvocation invocation);

        /// <summary>
        /// Intercepts an asynchronous method invocation without a return value.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        void InterceptAsynchronous(IInvocation invocation);

        /// <summary>
        /// Intercepts an asynchronous method invocation with a return value.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value.</typeparam>
        /// <param name="invocation">The method invocation.</param>
        void InterceptAsynchronous<TResult>(IInvocation invocation);
    }
}
