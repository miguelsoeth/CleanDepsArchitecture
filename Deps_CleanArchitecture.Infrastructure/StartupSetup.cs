using Deps_CleanArchitecture.Infrastructure.Data;
using Deps_CleanArchitecture.Infrastructure.Identity;
using Deps_CleanArchitecture.SharedKernel.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Deps_CleanArchitecture.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddDbContext(this IServiceCollection services) =>
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(AmbienteUtil.GetValue("POSTGRES_CONNECTION"))); // will be created in web project root

        public static void ConfigureJwt(this IServiceCollection services) => JwtStartupSetup.RegisterJWT(services);
    }
}
