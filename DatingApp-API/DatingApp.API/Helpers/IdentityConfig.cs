using Microsoft.Extensions.DependencyInjection;

namespace DatingApp.API.Helpers
{
    public static class IdentityConfig
    {
        public static void IdentityConfiguration(this IServiceCollection service)
        {
            service.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
                options.AddPolicy("VipOnly", policy => policy.RequireRole("VIP"));
            });
        }

    }
}
