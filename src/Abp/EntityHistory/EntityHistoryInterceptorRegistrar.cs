using System;
using System.Linq;
using System.Reflection;
using Abp.Dependency;
using Abp.Domain.Uow;

namespace Abp.EntityHistory
{
    internal static class EntityHistoryInterceptorRegistrar
    {
        public static void Initialize(IIocManager iocManager)
        {
            // Note: Interceptors are now automatically applied via BasicConventionalRegistrar
            // using Autofac.Extras.DynamicProxy's EnableInterfaceInterceptors
            // This method is kept for backwards compatibility but does nothing
        }
        
        private static bool ShouldIntercept(IEntityHistoryConfiguration entityHistoryConfiguration, Type type)
        {
            if (type.GetTypeInfo().IsDefined(typeof(UseCaseAttribute), true))
            {
                return true;
            }

            if (type.GetMethods().Any(m => m.IsDefined(typeof(UseCaseAttribute), true)))
            {
                return true;
            }

            return false;
        }
    }
}
