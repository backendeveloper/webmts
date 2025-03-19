using System.Reflection;
using Autofac;
using MediatR;
using NotificationService.Business.Consumers;
using NotificationService.Business.Renderers;
using NotificationService.Business.Senders;
using NotificationService.Business.Senders.Interfaces;
using NotificationService.Data;
using Module = Autofac.Module;

namespace NotificationService.Business;

public class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterModule<ServiceProxyModule>();
        builder.RegisterModule<DataModule>();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces();

        builder.RegisterType<NotificationService.Business.Services.NotificationService>().AsSelf()
            .InstancePerLifetimeScope();
        builder.RegisterType<SimpleTemplateRenderer>().As<ITemplateRenderer>().InstancePerLifetimeScope();

        builder.RegisterType<EmailSender>().As<INotificationSender>().InstancePerLifetimeScope();
        builder.RegisterType<SmsSender>().As<INotificationSender>().InstancePerLifetimeScope();
        builder.RegisterType<PushNotificationSender>().As<INotificationSender>().InstancePerLifetimeScope();
        builder.RegisterType<SystemNotificationSender>().As<INotificationSender>().InstancePerLifetimeScope();
        builder.RegisterType<NotificationSenderFactory>().InstancePerLifetimeScope();
        
        builder.RegisterType<SimpleTemplateRenderer>().As<ITemplateRenderer>().InstancePerLifetimeScope();
        builder.RegisterType<Services.NotificationService>().InstancePerLifetimeScope();
        builder.RegisterType<CustomerEventConsumer>().InstancePerLifetimeScope();
        builder.RegisterType<TransactionEventConsumer>().InstancePerLifetimeScope();
        builder.RegisterType<PendingNotificationsProcessor>().InstancePerLifetimeScope();
    }
}