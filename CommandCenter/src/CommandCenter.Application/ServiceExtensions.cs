using CommandCenter.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommandCenter.Application;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SessionService>();
        return services;
    }
}
