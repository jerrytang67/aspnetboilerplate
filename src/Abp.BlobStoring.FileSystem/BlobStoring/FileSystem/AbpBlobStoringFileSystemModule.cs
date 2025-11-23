using System.Reflection;
using Abp.Modules;

namespace Abp.BlobStoring.FileSystem
{
    [DependsOn(typeof(AbpBlobStoringModule))]
    public class AbpBlobStoringFileSystemModule : AbpModule
    {
        public override void ConfigureServices() {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }

        public override void Initialize()
        {
        }
    }
}
