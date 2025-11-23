namespace Abp.Dependency
{
    /// <summary>
    /// Generic wrapper for ABP async interceptors that provides type-safe registration.
    /// This class wraps an IAsyncInterceptor with AsyncDeterminationInterceptor for use with Autofac.
    /// </summary>
    /// <typeparam name="TInterceptor">The type of the async interceptor.</typeparam>
    public class AbpAsyncDeterminationInterceptor<TInterceptor> : AsyncDeterminationInterceptor
        where TInterceptor : IAsyncInterceptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbpAsyncDeterminationInterceptor{TInterceptor}"/> class.
        /// </summary>
        /// <param name="asyncInterceptor">The async interceptor to wrap.</param>
        public AbpAsyncDeterminationInterceptor(TInterceptor asyncInterceptor) : base(asyncInterceptor)
        {
        }
    }
}