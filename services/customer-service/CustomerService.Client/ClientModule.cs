using Autofac;
using CustomerService.Business;
using CustomerService.Common.Pipelines;
using FluentValidation;
using MediatR;

namespace CustomerService.Client;

public class ClientModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<BusinessModule>();

        builder.RegisterType<Mediator>().As<IMediator>().InstancePerLifetimeScope();

        builder.RegisterGeneric(typeof(LoggingPipelineBehavior<,>)).As(typeof(IPipelineBehavior<,>))
            .InstancePerLifetimeScope();
        builder.RegisterGeneric(typeof(InputValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(BusinessValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>))
            .InstancePerLifetimeScope();

        builder
            .RegisterAssemblyTypes(typeof(ClientModule).Assembly)
            .Where(t => t.IsClosedTypeOf(typeof(IValidator<>)))
            .AsImplementedInterfaces();
    }
}