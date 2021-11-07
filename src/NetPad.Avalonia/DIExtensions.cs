using System;
using Splat;

namespace NetPad
{
    public static class DIExtensions
    {
        public static TService GetRequiredService<TService>(this IReadonlyDependencyResolver resolver)
        {
            return (TService) GetRequiredService(resolver, typeof(TService));
        }
        
        public static object GetRequiredService(this IReadonlyDependencyResolver resolver, Type serviceType)
        {
            var service = resolver.GetService(serviceType);
            if (service is null)
            {
                throw new System.InvalidOperationException($"Failed to resolve object of type {serviceType}");
            }

            return service; 
        }
    }
}