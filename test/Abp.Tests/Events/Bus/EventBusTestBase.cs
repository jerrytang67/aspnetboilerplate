using Abp.Events.Bus;
using Microsoft.Extensions.Logging;

namespace Abp.Tests.Events.Bus {
    public abstract class EventBusTestBase {
        protected IEventBus EventBus;

        protected EventBusTestBase() {
            EventBus = new EventBus(null);
        }
    }
}