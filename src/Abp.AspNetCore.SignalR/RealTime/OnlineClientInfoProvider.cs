using System;
using Microsoft.Extensions.Logging;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Auditing;
using Abp.Logging;
using Microsoft.AspNetCore.SignalR;

namespace Abp.RealTime;

public class OnlineClientInfoProvider : IOnlineClientInfoProvider {
    private readonly IClientInfoProvider _clientInfoProvider;

    public OnlineClientInfoProvider(IClientInfoProvider clientInfoProvider, ILogger<OnlineClientInfoProvider> logger) {
        _clientInfoProvider = clientInfoProvider;
        Logger = logger;
    }

    public ILogger Logger { get; set; }

    public IOnlineClient CreateClientForCurrentConnection(HubCallerContext context) {
        return new OnlineClient(
            context.ConnectionId,
            GetIpAddressOfClient(context),
            context.GetTenantId(),
            context.GetUserIdOrNull()
        );
    }

    private string GetIpAddressOfClient(HubCallerContext context) {
        try {
            return _clientInfoProvider.ClientIpAddress;
        }
        catch (Exception ex) {
            Logger.LogError("Can not find IP address of the client! connectionId: " + context.ConnectionId);
            Logger.LogError(ex.Message, ex);
            return "";
        }
    }
}