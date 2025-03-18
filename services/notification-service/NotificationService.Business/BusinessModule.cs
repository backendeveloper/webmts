using System.Reflection;
using Autofac;
using MediatR;
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
        
        // builder.RegisterType<Services.NotificationService>().As<ITokenService>().InstancePerLifetimeScope();
        // builder.RegisterType<Services.AuthService>().As<IAuthService>().InstancePerLifetimeScope();
    }
}