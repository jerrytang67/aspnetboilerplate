using System;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Threading.Timers;
using Abp.ZeroCore.SampleApp.Core;
using Shouldly;
using Xunit;

namespace Abp.Zero.Users;

public class UserTokenExpirationWorker_Tests : AbpZeroTestBase {
    private readonly MyUserTokenExpirationWorker _userTokenExpirationWorker;
    private readonly IRepository<UserToken, long> _userTokenRepository;
    private readonly AbpUserManager<Role, User> _abpUserManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UserTokenExpirationWorker_Tests() {
        _userTokenExpirationWorker = Resolve<MyUserTokenExpirationWorker>();
        _userTokenRepository = Resolve<IRepository<UserToken, long>>();
        _abpUserManager = Resolve<AbpUserManager<Role, User>>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task Should_Remove_Expired_TokenValidityKeys() {
        //Arrange
        using (_unitOfWorkManager.Begin()) {
            var user = await _abpUserManager.GetUserByIdAsync(AbpSession.GetUserId());

            await _abpUserManager.AddTokenValidityKeyAsync(
                user,
                Guid.NewGuid().ToString(),
                DateTime.UtcNow
            );

            await _abpUserManager.AddTokenValidityKeyAsync(
                user,
                Guid.NewGuid().ToString(),
                DateTime.UtcNow.AddDays(1)
            );

            await _abpUserManager.AddTokenValidityKeyAsync(
                user,
                Guid.NewGuid().ToString(),
                DateTime.UtcNow.AddDays(1)
            );

            await _unitOfWorkManager.Current.SaveChangesAsync();

            var allTokens = await _userTokenRepository.GetAllListAsync(t => t.UserId == user.Id);
            allTokens.Count.ShouldBe(3);
        }

        using (_unitOfWorkManager.Begin()) {
            using (_unitOfWorkManager.Current.SetTenantId(null)) {
                var user = await _abpUserManager.FindByNameOrEmailAsync(AbpUserBase.AdminUserName);

                await _abpUserManager.AddTokenValidityKeyAsync(
                    user,
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow
                );

                await _abpUserManager.AddTokenValidityKeyAsync(
                    user,
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow.AddDays(1)
                );

                await _abpUserManager.AddTokenValidityKeyAsync(
                    user,
                    Guid.NewGuid().ToString(),
                    DateTime.UtcNow.AddDays(1)
                );

                await _unitOfWorkManager.Current.SaveChangesAsync();

                var allTokens = await _userTokenRepository.GetAllListAsync(t => t.UserId == user.Id);
                allTokens.Count.ShouldBe(3);
            }
        }

        //Act
        _userTokenExpirationWorker.Start();

        //Assert
        using (_unitOfWorkManager.Begin()) {
            var user = await _abpUserManager.GetUserByIdAsync(AbpSession.GetUserId());
            var allTokens = await _userTokenRepository.GetAllListAsync(t => t.UserId == user.Id);
            allTokens.Count.ShouldBe(2);
        }

        using (_unitOfWorkManager.Begin()) {
            using (_unitOfWorkManager.Current.SetTenantId(null)) {
                var user = await _abpUserManager.FindByNameOrEmailAsync(AbpUserBase.AdminUserName);
                var allTokens = await _userTokenRepository.GetAllListAsync(t => t.UserId == user.Id);
                allTokens.Count.ShouldBe(2);
            }
        }
    }
}

internal class MyUserTokenExpirationWorker(
    AbpTimer timer,
    IRepository<UserToken, long> userTokenRepository,
    IBackgroundJobConfiguration backgroundJobConfiguration,
    IUnitOfWorkManager unitOfWorkManager,
    IRepository<Tenant> tenantRepository,
    IIocManager iocManager)
    : UserTokenExpirationWorker<Tenant, User>(timer, userTokenRepository, backgroundJobConfiguration,
        unitOfWorkManager, tenantRepository, iocManager) {
    public override void Start() {
        DoWork();
    }
}