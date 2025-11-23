using System.Reflection;
using Abp.AspNetCore.SignalR.Notifications;
using Abp.Modules;

namespace Abp.AspNetCore.SignalR;

/// <summary>
/// ABP ASP.NET Core SignalR integration module.
/// </summary>
[DependsOn(typeof(AbpKernelModule))]
public class AbpAspNetCoreSignalRModule : AbpModule {
    public override void ConfigureServices() {
        IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

        Configuration.Notifications.Notifiers.Add<SignalRRealTimeNotifier>();
    }

    /// <inheritdoc/>
    public override void Initialize() {
    }
}