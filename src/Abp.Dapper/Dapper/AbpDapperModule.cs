using Abp.Dependency;
using Abp.Modules;
using Abp.Orm;
using Abp.Reflection.Extensions;
using Slapper;

namespace Abp.Dapper {
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpDapperModule : AbpModule {
        public override void ConfigureServices() {
            IocManager.RegisterAssemblyByConvention(typeof(AbpDapperModule).GetAssembly());
        }


        public override void Initialize() {
            Configuration.UnitOfWork.IsTransactionScopeAvailable = false;
            AutoMapper.Configuration.TypeConverters.Add(new AutoMapper.Configuration.EnumConverter());

            using (IScopedIocResolver scope = IocManager.CreateScope()) {
                ISecondaryOrmRegistrar[] additionalOrmRegistrars = scope.ResolveAll<ISecondaryOrmRegistrar>();

                foreach (ISecondaryOrmRegistrar registrar in additionalOrmRegistrars) {
                    if (registrar.OrmContextKey == AbpConsts.Orms.EntityFramework) {
                        registrar.RegisterRepositories(IocManager, EfBasedDapperAutoRepositoryTypes.Default);
                    }

                    if (registrar.OrmContextKey == AbpConsts.Orms.NHibernate) {
                        registrar.RegisterRepositories(IocManager, NhBasedDapperAutoRepositoryTypes.Default);
                    }

                    if (registrar.OrmContextKey == AbpConsts.Orms.EntityFrameworkCore) {
                        registrar.RegisterRepositories(IocManager, EfBasedDapperAutoRepositoryTypes.Default);
                    }
                }
            }
        }
    }
}