using System.Reflection;
using Autofac;
using MediatR;
using TransactionService.Data;
using Module = Autofac.Module;

namespace TransactionService.Business;

public class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterModule<ServiceProxyModule>();
        builder.RegisterModule<DataModule>();
        
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces();
    }
}