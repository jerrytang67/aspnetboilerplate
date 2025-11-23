using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Abp.Runtime;
using Abp.Logging;

namespace Abp.Threading {
    public abstract class CancellationTokenProviderBase : ICancellationTokenProvider {
        public const string CancellationTokenOverrideContextKey = "Abp.Threading.CancellationToken.Override";

        public abstract CancellationToken Token { get; }

        public ILogger Logger { get; set; }

        protected IAmbientScopeProvider<CancellationTokenOverride> CancellationTokenOverrideScopeProvider { get; }

        protected CancellationTokenOverride OverridedValue => CancellationTokenOverrideScopeProvider.GetValue(CancellationTokenOverrideContextKey);

        protected CancellationTokenProviderBase(IAmbientScopeProvider<CancellationTokenOverride> cancellationTokenOverrideScopeProvider, ILogger logger) {
            CancellationTokenOverrideScopeProvider = cancellationTokenOverrideScopeProvider;
            Logger = logger;
        }

        public IDisposable Use(CancellationToken cancellationToken) {
            return CancellationTokenOverrideScopeProvider.BeginScope(CancellationTokenOverrideContextKey, new CancellationTokenOverride(cancellationToken));
        }
    }
}