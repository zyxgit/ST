using System.Reflection;
using ST.Infra.Repository.Interceptor;
using ST.Shared.Dependency;

namespace ST.Shared.WebApi.Autofac;

public static class DependencyRegistrar
{
    private static readonly Type _transientType = typeof(ITransientDependency);
    private static readonly Type _singletonType = typeof(ISingletonDependency);
    private static readonly Type _scopedType = typeof(IScopedDependency);

    public static void RegisterDependencies(this ContainerBuilder builder, params Assembly[] assemblies)
    {
        // Transient
        builder.RegisterAssemblyTypes(assemblies)
               .Where(t => _transientType.IsAssignableFrom(t)
                           && t.IsClass && !t.IsAbstract)
               .AsImplementedInterfaces()
               .AsSelf()
               .InstancePerDependency();

        // Singleton
        builder.RegisterAssemblyTypes(assemblies)
               .Where(t => _singletonType.IsAssignableFrom(t)
                           && t.IsClass && !t.IsAbstract)
               .AsImplementedInterfaces()
               .AsSelf()
               .SingleInstance();

        // Scoped
        builder.RegisterAssemblyTypes(assemblies)
               .Where(t => _scopedType.IsAssignableFrom(t)
                           && t.IsClass && !t.IsAbstract)
               .AsImplementedInterfaces()
               .AsSelf()
               .InstancePerLifetimeScope();
    }
}
