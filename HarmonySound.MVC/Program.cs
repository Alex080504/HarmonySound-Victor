using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HarmonySound.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configuraci�n de los endpoints de la API
            Crud<Album>.EndPoint = "https://localhost:7120/api/Albums";
            Crud<ContentAlbum>.EndPoint = "https://localhost:7120/api/ContentAlbums";
            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            Crud<Plan>.EndPoint = "https://localhost:7120/api/Plans";
            Crud<Report>.EndPoint = "https://localhost:7120/api/Reports";
            Crud<Role>.EndPoint = "https://localhost:7120/api/Roles";
            Crud<Statistic>.EndPoint = "https://localhost:7120/api/Statistics";
            Crud<SubscriptionHistory>.EndPoint = "https://localhost:7120/api/SubscriptionsHistories";
            Crud<UserRole>.EndPoint = "https://localhost:7120/api/UserRoles";
            Crud<User>.EndPoint = "https://localhost:7120/api/Users";
            Crud<UserPlan>.EndPoint = "https://localhost:7120/api/UsersPlans";

            // Configuraci�n de Serilog para registrar en la consola y en un archivo
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // Imprime en la consola
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day) // Guarda los logs en un archivo
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // Configuraci�n de los l�mites de tama�o de solicitud
            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 104857600; // 100 MB
            });

            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 104857600; // 100 MB
            });

            // Configuraci�n de logging (se usa Serilog)
            builder.Logging.ClearProviders();  // Limpiar proveedores de logs predeterminados
            builder.Logging.AddSerilog();  // Usar Serilog para los logs

            // Configuraci�n de autenticaci�n por cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // Ruta para login
                    options.LogoutPath = "/Account/Logout"; // Ruta para logout
                });

            // Configuraci�n de sesiones en memoria (sin DbContext en MVC)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo m�ximo de inactividad
                options.Cookie.HttpOnly = true; // Hace que la cookie no sea accesible por JS
                options.Cookie.IsEssential = true; // Hace la cookie esencial
            });

            // Agregar controladores y vistas
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession();  // Asegura que la sesi�n est� disponible

            // Inyectar HttpClient para hacer solicitudes a la API
            builder.Services.AddHttpClient(); // Esto permite usar HttpClient en tus controladores

            var app = builder.Build();

            // Configuraci�n de la tuber�a HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error"); // P�gina de error para producci�n
                app.UseHsts();  // HSTS para seguridad adicional en producci�n
            }

            app.UseHttpsRedirection();  // Redirecci�n a HTTPS
            app.UseStaticFiles();  // Permite archivos est�ticos

            app.UseRouting();  // Usar el enrutamiento
            app.UseSession();  // Usar sesiones

            // Autenticaci�n y autorizaci�n
            app.UseAuthentication();
            app.UseAuthorization();

            // Configurar la ruta de los controladores
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}"); // Ruta por defecto para Login

            // Global exception handling middleware
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    if (exceptionHandlerPathFeature?.Error != null)
                    {
                        // Registra los errores no manejados en el archivo de logs
                        Log.Error(exceptionHandlerPathFeature.Error, "Unhandled exception occurred.");
                    }

                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An error occurred.");
                });
            });

            // Ejecutar la aplicaci�n
            app.Run();
        }
    }
}
