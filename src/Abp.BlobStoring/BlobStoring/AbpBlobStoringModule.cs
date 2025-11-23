using System.Reflection;
using Abp.Dependency;
using Abp.Modules;

namespace Abp.BlobStoring
{
    public class AbpBlobStoringModule : AbpModule
    {
        public override void ConfigureServices() {
            IocManager.Register<AbpBlobStoringOptions>();

            IocManager.Register(typeof(IBlobContainer<>), typeof(BlobContainer<>), DependencyLifeStyle.Transient);
            IocManager.Register<IBlobContainer, BlobContainer<DefaultContainer>>(DependencyLifeStyle.Transient);

            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
        public override void Initialize()
        {
        }
    }
}
