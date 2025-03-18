using Autofac;
using TransactionService.Data.Repositories;
using TransactionService.Data.Repositories.Imp;

namespace TransactionService.Data;

public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<TransactionRepository>().As<ITransactionRepository>().InstancePerLifetimeScope();
        builder.RegisterType<TransactionHistoryRepository>().As<ITransactionHistoryRepository>().InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
    }
}