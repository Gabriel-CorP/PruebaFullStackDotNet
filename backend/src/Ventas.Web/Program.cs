using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Ventas.Web.Infrastructure;
using Ventas.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Cliente tipado de la API (con JWT automático) ----------
var apiBase = builder.Configuration["Api:BaseUrl"]
              ?? throw new InvalidOperationException("Falta la configuración 'Api:BaseUrl'.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services
    .AddHttpClient<IVentasApiClient, VentasApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

// ---------- Autenticación por cookie (el JWT de la API viaja dentro de la cookie cifrada) ----------
static bool EsAjax(HttpRequest r) => r.Headers.XRequestedWith == "XMLHttpRequest";

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Ventas.Auth";
        options.Cookie.HttpOnly = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = false; // la vigencia la define el JWT (ExpiresUtc al iniciar sesión)

        // Para AJAX devolvemos 401/403 en vez de redirigir a una página HTML.
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (EsAjax(ctx.Request)) ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (EsAjax(ctx.Request)) ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Todo requiere sesión salvo lo marcado con [AllowAnonymous]
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddControllersWithViews(options => options.Filters.Add<ApiExceptionFilter>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Moneda y decimales consistentes (USD, punto decimal)
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US")
    .AddSupportedUICultures("en-US"));

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
