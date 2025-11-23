using Abp.Dependency;

namespace Abp
{
    public class AbpBootstrapperOptions
    {
        /// <summary>
        /// Used to disable all interceptors added by ABP.
        /// </summary>
        public AbpBootstrapperInterceptorOptions InterceptorOptions { get; set; }

        /// <summary>
        /// IIocManager that is used to bootstrap the ABP system. If set to null, uses global <see cref="Abp.Dependency.IocManager.Instance"/>
        /// </summary>
        public IIocManager IocManager { get; set; }

        public AbpBootstrapperOptions()
        {
            IocManager = Abp.Dependency.IocManager.Instance;
            InterceptorOptions = new AbpBootstrapperInterceptorOptions();
        }
    }

    public class AbpBootstrapperInterceptorOptions
    {
        public bool DisableValidationInterceptor { get; set; }
        
        public bool DisableAuditingInterceptor { get; set; }
        
        public bool DisableEntityHistoryInterceptor { get; set; }
        
        public bool DisableUnitOfWorkInterceptor { get; set; }
        
        public bool DisableAuthorizationInterceptor { get; set; }
    }
}
