using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureChat.BLL.Interfaces;
using SecureChat.BLL.Services;
using SecureChat.BLL.Settings;
using SecureChat.DAL;

namespace SecureChat.BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBllServices(this IServiceCollection services, IConfiguration configuration)
        {
            // calling DAL Services
            services.AddDalServices(configuration);

            // reading confidential settings
            services.Configure<SecuritySettings>(configuration.GetSection("SecuritySettings"));
            services.Configure<CryptoSettings>(configuration.GetSection("CryptoSettings"));

            // inject BLL services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICryptoService, CryptoService>();
            services.AddScoped<IChatService, ChatService>();

            return services;
        }
    }
}