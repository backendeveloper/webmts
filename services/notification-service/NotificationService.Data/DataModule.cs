using Autofac;
using NotificationService.Data.Repositories;
using NotificationService.Data.Repositories.Imp;

namespace NotificationService.Data;

public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NotificationTemplateRepository>().As<INotificationTemplateRepository>().InstancePerLifetimeScope();
        builder.RegisterType<NotificationRepository>().As<INotificationRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
    }
}