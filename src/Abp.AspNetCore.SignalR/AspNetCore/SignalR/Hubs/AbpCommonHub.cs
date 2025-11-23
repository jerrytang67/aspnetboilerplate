using Abp.Auditing;
using Abp.RealTime;
using Microsoft.Extensions.Logging;

namespace Abp.AspNetCore.SignalR.Hubs;

public class AbpCommonHub(IOnlineClientManager onlineClientManager, IOnlineClientInfoProvider clientInfoProvider, ILogger<AbpCommonHub> logger)
    : OnlineClientHubBase(onlineClientManager,
        clientInfoProvider, logger) {
    public void Register() {
        logger.LogDebug("A client is registered: " + Context.ConnectionId);
    }
}