using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Abp.Dependency
{
    /// <summary>
    /// Determines whether a method is synchronous or asynchronous and delegates to the appropriate
    /// interception method on an IAsyncInterceptor.
    /// This is ABP's own implementation compatible with Autofac.Extras.DynamicProxy.
    /// </summary>
    public class AsyncDeterminationInterceptor : IInterceptor
    {
        private readonly IAsyncInterceptor _asyncInterceptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDeterminationInterceptor"/> class.
        /// </summary>
        /// <param name="asyncInterceptor">The async interceptor to delegate to.</param>
        public AsyncDeterminationInterceptor(IAsyncInterceptor asyncInterceptor)
        {
            _asyncInterceptor = asyncInterceptor ?? throw new ArgumentNullException(nameof(asyncInterceptor));
        }

        /// <summary>
        /// Intercepts a method invocation and determines whether it's synchronous or asynchronous.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        public void Intercept(IInvocation invocation)
        {
            var method = invocation.Method;

            // Check if the method returns Task or Task<T>
            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                // Asynchronous method
                if (method.ReturnType == typeof(Task))
                {
                    // Task without result
                    _asyncInterceptor.InterceptAsynchronous(invocation);
                }
                else if (method.ReturnType.IsGenericType && 
                         method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // Task<TResult>
                    var resultType = method.ReturnType.GetGenericArguments()[0];
                    var interceptMethod = typeof(IAsyncInterceptor)
                        .GetMethod(nameof(IAsyncInterceptor.InterceptAsynchronous), 
                                   BindingFlags.Public | BindingFlags.Instance)
                        .MakeGenericMethod(resultType);
                    
                    interceptMethod.Invoke(_asyncInterceptor, new object[] { invocation });
                }
                else
                {
                    // Some other Task-like type, treat as synchronous
                    _asyncInterceptor.InterceptSynchronous(invocation);
                }
            }
            else
            {
                // Synchronous method
                _asyncInterceptor.InterceptSynchronous(invocation);
            }
        }
    }
}
