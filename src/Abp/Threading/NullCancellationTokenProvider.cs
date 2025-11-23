using System.Threading;
using Abp.Logging;
using Abp.Runtime.Remoting;

namespace Abp.Threading {
    public class NullCancellationTokenProvider : CancellationTokenProviderBase {
        public static NullCancellationTokenProvider Instance { get; } = new NullCancellationTokenProvider();

        public override CancellationToken Token => CancellationToken.None;

        private NullCancellationTokenProvider()
            : base(
                new DataContextAmbientScopeProvider<CancellationTokenOverride>(new AsyncLocalAmbientDataContext(), null), null) {
        }
    }
}