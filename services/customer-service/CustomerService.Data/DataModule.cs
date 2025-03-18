using Autofac;
using CustomerService.Data.Repositories;
using CustomerService.Data.Repositories.Imp;

namespace CustomerService.Data;

public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CustomerRepository>().As<ICustomerRepository>().InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
    }
}