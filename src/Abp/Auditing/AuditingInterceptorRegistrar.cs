using System;
using System.Linq;
using System.Reflection;
using Abp.Dependency;

namespace Abp.Auditing
{
    internal static class AuditingInterceptorRegistrar
    {
        public static void Initialize(IIocManager iocManager)
        {
            // Note: Interceptors are now automatically applied via BasicConventionalRegistrar
            // using Autofac.Extras.DynamicProxy's EnableInterfaceInterceptors
            // This method is kept for backwards compatibility but does nothing
        }
        
        private static bool ShouldIntercept(IIocManager iocManager, Type type)
        {
            if (type.GetTypeInfo().IsDefined(typeof(AuditedAttribute), true))
            {
                return true;
            }

            if (type.GetMethods().Any(m => m.IsDefined(typeof(AuditedAttribute), true)))
            {
                return true;
            }

            if (!iocManager.IsRegistered<IAbpAuditingDefaultOptions>())
            {
                return false;
            }
            
            var auditingOptions = iocManager.Resolve<IAbpAuditingDefaultOptions>();
            
            if (auditingOptions.ConventionalAuditingSelectors.Any(selector => selector(type)))
            {
                return true;
            }
            
            return false;
        }
    }
}
