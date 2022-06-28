using System.Security.Claims;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddAuthorization(o => { o.AddPolicy("auth", p => p.RequireAuthenticatedUser()); });

builder.Services.AddAuthentication(o => { o.DefaultScheme = "Cookies"; })
    .AddCookie("Cookies", o =>
    {
        o.Cookie.Name = ".myapp";
        o.Cookie.Domain = "localhost";
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.DataProtectionProvider = DataProtectionProvider.Create("yarp-test");
    });

var app = builder.Build();

app.UseForwardedHeaders();
app.Use((context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var pathBase))
    {
        context.Request.PathBase = pathBase.ToString();
    }

    return next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (ClaimsPrincipal user, HttpContext ctx) =>
new
{
    name = user.GetName(),
    claims = user.Claims.Select(x => new { name = x.Type, value = x.Value }),
    host = ctx.Request.Host
})
.RequireAuthorization("auth");

app.Run();