using Autofac;
using FluentValidation;
using MediatR;
using TransactionService.Business;
using TransactionService.Common.Pipelines;

namespace TransactionService.Client;

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