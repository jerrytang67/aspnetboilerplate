using System.Linq;

using Abp.Dependency;
using Abp.Modules;

using Shouldly;

using Xunit;

namespace Abp.Tests.Modules
{
    public class StartupModuleToBeLast_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void StartupModule_ShouldBe_LastModule()
        {
            //Arrange
            var bootstrapper = AbpBootstrapper.Create<MyStartupModule>(options =>
            {
                options.IocManager = LocalIocManager;
            });


            // Build container before calling Initialize (two-phase lifecycle)
            ((IocManager)LocalIocManager).BuildContainer();
            bootstrapper.Initialize();

            //Act
            var modules = bootstrapper.IocManager.Resolve<IAbpModuleManager>().Modules;

            //Assert
            modules.Count.ShouldBe(4);

            modules.Any(m => m.Type == typeof(AbpKernelModule)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyStartupModule)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyModule1)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyModule2)).ShouldBeTrue();

            var startupModule = modules.Last();

            startupModule.Type.ShouldBe(typeof(MyStartupModule));
        }

        [Fact]
        public void PluginModule_ShouldNotBeLast()
        {
            var bootstrapper = AbpBootstrapper.Create<MyStartupModule>(options =>
            {
                options.IocManager = LocalIocManager;
            });


            // Build container before calling Initialize (two-phase lifecycle)
            ((IocManager)LocalIocManager).BuildContainer();
            bootstrapper.Initialize();

            var modules = bootstrapper.IocManager.Resolve<IAbpModuleManager>().Modules;

            //Assert
            modules.Count.ShouldBe(6);

            modules.Any(m => m.Type == typeof(AbpKernelModule)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyStartupModule)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyModule1)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyModule2)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyPlugInModule)).ShouldBeTrue();
            modules.Any(m => m.Type == typeof(MyPlugInDependedModule)).ShouldBeTrue();

            modules.Last().Type.ShouldBe(typeof(MyStartupModule));
        }

        [DependsOn(typeof(MyModule1), typeof(MyModule2))]
        public class MyStartupModule : AbpModule {}

        public class MyModule1 : AbpModule {}

        public class MyModule2 : AbpModule {}

        [DependsOn(typeof(MyPlugInDependedModule))]
        public class MyPlugInModule : AbpModule {}

        public class MyPlugInDependedModule : AbpModule {}
    }
}
