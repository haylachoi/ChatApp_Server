using System.Reflection;

namespace ChatApp_Server.Configs
{
    public static class RegisterServiceExtension
    {
        public static void RegisterAppService(this IServiceCollection services, string namespaceName)
        {
            var coupleTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && String.Equals(type.Namespace, namespaceName, StringComparison.Ordinal))
            .SelectMany(type => type.GetInterfaces()
                .Where(interfaceType =>
                {
                    return !interfaceType.IsGenericType && type.Name == interfaceType.Name.Substring(1);
                })
                .Select(interfaceType =>
                {
                    return (interfaceType, type);
                }));

        
            foreach (var (interfaceType, type) in coupleTypes)
            {
                services.AddScoped(interfaceType, type);
            }
        }
    }
}
