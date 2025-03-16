using AuthService.Business;
using Autofac;
using FluentValidation;
using MediatR;

namespace AuthService.Client;

public class ClientModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<BusinessModule>();
        
        builder.RegisterType<Mediator>().As<IMediator>().InstancePerLifetimeScope();
        
        // builder.RegisterGeneric(typeof(ExceptionHandlingBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
        // builder.RegisterGeneric(typeof(LoggingPipelineBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
        // builder.RegisterGeneric(typeof(CacheBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
        // builder.RegisterGeneric(typeof(InputValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>));

        builder
            .RegisterAssemblyTypes(typeof(ClientModule).Assembly)
            .Where(t => t.IsClosedTypeOf(typeof(IValidator<>)))
            .AsImplementedInterfaces();
    }
}