using System;
using System.Reflection;
using Abp.Dependency;

namespace Abp.Runtime.Validation.Interception
{
    internal static class ValidationInterceptorRegistrar
    {
        public static void Initialize(IIocManager iocManager)
        {
            // Note: Interceptors are now automatically applied via BasicConventionalRegistrar
            // using Autofac.Extras.DynamicProxy's EnableInterfaceInterceptors
            // This method is kept for backwards compatibility but does nothing
        }
    }
}
