using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Notifications;
using Abp.Runtime.Session;
using Autofac;
using JetBrains.Annotations;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Abp.Zero.Notifications;

public class Notifier1 : IRealTimeNotifier
{
    public bool UseOnlyIfRequestedAsTarget => false;

    public bool IsSendNotificationCalled { get; set; }

    public async Task SendNotificationsAsync(UserNotification[] userNotifications)
    {
        IsSendNotificationCalled = true;

        // send email
        await Task.CompletedTask;
    }
}

public class Notifier2 : IRealTimeNotifier
{
    public bool UseOnlyIfRequestedAsTarget => false;

    public bool IsSendNotificationCalled { get; set; }

    public async Task SendNotificationsAsync(UserNotification[] userNotifications)
    {
        IsSendNotificationCalled = true;

        // send sms
        await Task.CompletedTask;
    }
}

public class RealtimeNotification_TargetNotification_Tests : AbpZeroTestBase
{
    private readonly INotificationPublisher _publisher;
    private Notifier1 _realTimeNotifier1;
    private Notifier2 _realTimeNotifier2;

    protected override void PreInitialize()
    {
        base.PreInitialize();
        
        var defaultNotificationDistributor = LocalIocManager.Resolve<DefaultNotificationDistributer>();
        var iocMgr = (Abp.Dependency.IocManager)LocalIocManager;
        iocMgr.Builder.RegisterInstance(defaultNotificationDistributor)
            .As<INotificationDistributer>()
            .SingleInstance();

        _realTimeNotifier1 = new Notifier1();
        _realTimeNotifier2 = new Notifier2();

        var realTimeNotifierType1 = _realTimeNotifier1.GetType();
        var realTimeNotifierType2 = _realTimeNotifier2.GetType();

        iocMgr.Builder.RegisterInstance(_realTimeNotifier1)
            .As(realTimeNotifierType1)
            .SingleInstance();
        iocMgr.Builder.RegisterInstance(_realTimeNotifier2)
            .As(realTimeNotifierType2)
            .SingleInstance();

        var notificationConfiguration = LocalIocManager.Resolve<INotificationConfiguration>();
        notificationConfiguration.Notifiers.Add(realTimeNotifierType1);
        notificationConfiguration.Notifiers.Add(realTimeNotifierType2);
    }

    public RealtimeNotification_TargetNotification_Tests()
    {
        _publisher = LocalIocManager.Resolve<INotificationPublisher>();
    }

    [Fact]
    public async Task Should_Send_Notification_To_All_Notifiers()
    {
        _realTimeNotifier1.IsSendNotificationCalled = false;
        _realTimeNotifier2.IsSendNotificationCalled = false;

        var notificationData = new NotificationData();

        await _publisher.PublishAsync("TestNotification",
            notificationData,
            severity: NotificationSeverity.Success,
            userIds: new[] { AbpSession.ToUserIdentifier() }
        );

        Assert.True(_realTimeNotifier1.IsSendNotificationCalled);
        Assert.True(_realTimeNotifier2.IsSendNotificationCalled);
    }

    [Fact]
    public async Task Should_Send_Notification_To_All_Defined_Notifiers()
    {
        _realTimeNotifier1.IsSendNotificationCalled = false;
        _realTimeNotifier2.IsSendNotificationCalled = false;

        var notificationData = new NotificationData();

        await _publisher.PublishAsync("TestNotification",
            notificationData,
            severity: NotificationSeverity.Success,
            userIds: new[] { AbpSession.ToUserIdentifier() },
            targetNotifiers: new[] { _realTimeNotifier1.GetType(), _realTimeNotifier2.GetType() }
        );

        Assert.True(_realTimeNotifier1.IsSendNotificationCalled);
        Assert.True(_realTimeNotifier2.IsSendNotificationCalled);
    }

    [Fact]
    public async Task Should_Send_Notification_To_Defined_Notifiers()
    {
        _realTimeNotifier1.IsSendNotificationCalled = false;
        _realTimeNotifier2.IsSendNotificationCalled = false;

        var notificationData = new NotificationData();

        await _publisher.PublishAsync("TestNotification",
            notificationData,
            severity: NotificationSeverity.Success,
            userIds: new[] { AbpSession.ToUserIdentifier() },
            targetNotifiers: new[] { _realTimeNotifier2.GetType() }
        );

        Assert.False(_realTimeNotifier1.IsSendNotificationCalled);
        Assert.True(_realTimeNotifier2.IsSendNotificationCalled);

        _realTimeNotifier1.IsSendNotificationCalled = false;
        _realTimeNotifier2.IsSendNotificationCalled = false;

        await _publisher.PublishAsync("TestNotification",
            notificationData,
            severity: NotificationSeverity.Success,
            userIds: new[] { AbpSession.ToUserIdentifier() },
            targetNotifiers: new[] { _realTimeNotifier1.GetType() }
        );

        Assert.True(_realTimeNotifier1.IsSendNotificationCalled);
        Assert.False(_realTimeNotifier2.IsSendNotificationCalled);
    }

    [Fact]
    [CanBeNull]
    public async Task Should_Throw_Exception_If_Notifier_Not_Registered()
    {
        var notificationData = new NotificationData();

        _realTimeNotifier1.IsSendNotificationCalled = false;
        _realTimeNotifier2.IsSendNotificationCalled = false;

        await Should.ThrowAsync<Exception>(async () =>
        {
            await _publisher.PublishAsync("TestNotification",
                notificationData,
                severity: NotificationSeverity.Success,
                userIds: new[] { AbpSession.ToUserIdentifier() },
                targetNotifiers: new[] { typeof(NotificationData) }
            );
        });

        Assert.False(_realTimeNotifier1.IsSendNotificationCalled);
        Assert.False(_realTimeNotifier2.IsSendNotificationCalled);

        var notifierSubstitute = Substitute.For<IRealTimeNotifier>();
        _realTimeNotifier1.IsSendNotificationCalled = false;
        _realTimeNotifier2.IsSendNotificationCalled = false;

        await Should.ThrowAsync<Exception>(async () =>
        {
            await _publisher.PublishAsync("TestNotification",
                notificationData,
                severity: NotificationSeverity.Success,
                userIds: new[] { AbpSession.ToUserIdentifier() },
                targetNotifiers: new[] { notifierSubstitute.GetType() }
            );
        });

        Assert.False(_realTimeNotifier1.IsSendNotificationCalled);
        Assert.False(_realTimeNotifier2.IsSendNotificationCalled);
    }
}