using AuthService.Data.Repositories;
using AuthService.Data.Repositories.Imp;
using Autofac;

namespace AuthService.Data;

public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UserRepository>().As<IUserRepository>().InstancePerLifetimeScope();
        builder.RegisterType<RefreshTokenRepository>().As<IRefreshTokenRepository>().InstancePerLifetimeScope();
        builder.RegisterType<RoleRepository>().As<IRoleRepository>().InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
    }
}