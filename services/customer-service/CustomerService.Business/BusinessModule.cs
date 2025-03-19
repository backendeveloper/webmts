using System.Reflection;
using Autofac;
using CustomerService.Business.Events;
using CustomerService.Data;
using MediatR;
using Module = Autofac.Module;

namespace CustomerService.Business;

public class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterModule<ServiceProxyModule>();
        builder.RegisterModule<DataModule>();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces();
        
        builder.RegisterType<RabbitMQEventBus>().As<IEventBus>().InstancePerLifetimeScope();
    }
}