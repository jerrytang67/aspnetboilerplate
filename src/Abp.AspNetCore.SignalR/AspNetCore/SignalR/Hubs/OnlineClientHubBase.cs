using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Abp.Dependency;
using Abp.RealTime;
using Abp.Runtime.Session;
using Abp.Logging;

namespace Abp.AspNetCore.SignalR.Hubs;

public abstract class OnlineClientHubBase : AbpHubBase, ITransientDependency
{
    private readonly ILogger<OnlineClientHubBase> _logger;
    protected IOnlineClientManager OnlineClientManager { get; }
    protected IOnlineClientInfoProvider OnlineClientInfoProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbpCommonHub"/> class.
    /// </summary>
    protected OnlineClientHubBase(
        IOnlineClientManager onlineClientManager,
        IOnlineClientInfoProvider clientInfoProvider,
        ILogger<OnlineClientHubBase> logger
        )
    {
        _logger = logger;
        OnlineClientManager = onlineClientManager;
        OnlineClientInfoProvider = clientInfoProvider;

#pragma warning disable CS0618 // Type or member is obsolete, this line will be removed once the AbpSession property is removed
        AbpSession = NullAbpSession.Instance;
#pragma warning restore CS0618 // Type or member is obsolete, this line will be removed once the AbpSession property is removed
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        var client = CreateClientForCurrentConnection();

        _logger.LogDebug("A client is connected: " + client);

        await OnlineClientManager.AddAsync(client);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await base.OnDisconnectedAsync(exception);

        _logger.LogDebug("A client is disconnected: " + Context.ConnectionId);

        try
        {
            await OnlineClientManager.RemoveAsync(Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.ToString(), ex);
        }
    }

    protected virtual IOnlineClient CreateClientForCurrentConnection()
    {
        return OnlineClientInfoProvider.CreateClientForCurrentConnection(Context);
    }
}