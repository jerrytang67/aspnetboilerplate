using Abp.Dependency;
using Abp.Threading;
using Microsoft.AspNetCore.Http;
using System.Threading;
using Abp.Runtime;
using Microsoft.Extensions.Logging;

namespace Abp.AspNetCore.Threading;

public class HttpContextCancellationTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    IAmbientScopeProvider<CancellationTokenOverride> cancellationTokenOverrideScopeProvider,
    ILogger<HttpContextCancellationTokenProvider> logger)
    : CancellationTokenProviderBase(cancellationTokenOverrideScopeProvider, logger), ITransientDependency {
    public override CancellationToken Token {
        get {
            if (OverridedValue != null) {
                return OverridedValue.CancellationToken;
            }

            return httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        }
    }
}