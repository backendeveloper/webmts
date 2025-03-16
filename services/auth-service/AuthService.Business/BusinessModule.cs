using System.Reflection;
using AuthService.Business.Services;
using Autofac;
using MediatR;
using Module = Autofac.Module;

namespace AuthService.Business;

public class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterModule<ServiceProxyModule>();
        
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces();
        
        // builder.RegisterGeneric(typeof(BusinessValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
        
        builder.RegisterType<InMemoryUserStore>().AsSelf().SingleInstance();
        builder.RegisterType<TokenService>().As<ITokenService>().InstancePerLifetimeScope();
        builder.RegisterType<Services.AuthService>().As<IAuthService>().InstancePerLifetimeScope();
    }
}