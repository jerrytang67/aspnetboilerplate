using System.Reflection;
using Abp.Modules;
using Abp.MongoDb.Configuration;

namespace Abp.MongoDb
{
    /// <summary>
    /// This module is used to implement "Data Access Layer" in MongoDB.
    /// </summary>
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpMongoDbModule : AbpModule
    {
        public override void ConfigureServices() {
            IocManager.Register<IAbpMongoDbModuleConfiguration, AbpMongoDbModuleConfiguration>();

            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }


        public override void Initialize()
        {
        }
    }
}
