using System.Reflection;
using AuthService.Business.Services;
using AuthService.Data;
using Autofac;
using MediatR;
using Module = Autofac.Module;

namespace AuthService.Business;

public class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterModule<ServiceProxyModule>();
        builder.RegisterModule<DataModule>();
        
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces();
        
        // builder.RegisterGeneric(typeof(BusinessValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
        
        builder.RegisterType<TokenService>().As<ITokenService>().InstancePerLifetimeScope();
        builder.RegisterType<Services.AuthService>().As<IAuthService>().InstancePerLifetimeScope();
    }
}