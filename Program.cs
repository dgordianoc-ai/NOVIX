using Microsoft.Data.SqlClient;

namespace NOVIX
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<SqlConnection>(serviceProvider =>
            {
                var configuration =
                    serviceProvider.GetRequiredService<IConfiguration>();

                var connectionString =
                    configuration.GetConnectionString("NOVIXConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "No se encontró la cadena de conexión NOVIXConnection.");
                }

                return new SqlConnection(connectionString);
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}