using AbyssScan.Core.Interfaces;
using AbyssScan.Services;
using AbyssScan.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var basePath = AppContext.BaseDirectory;
var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("Infrastructure/Config/config.json", optional: false, reloadOnChange: true)
    .Build();

builder.Services.AddSingleton<IConfiguration>(configuration);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new InMemoryLogProvider());

builder.Services.AddSingleton<HtmlParser>();
builder.Services.AddSingleton<IFormScanner, FormScanner>();

builder.Services.AddSingleton<IThrottlingService>(provider =>
{
    return new ThrottlingService();
});

builder.Services.AddScoped<IFormScanner, FormScanner>();

builder.Services.AddSingleton<IDictionaryProvider>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<FileDictionaryProvider>>();
    return new FileDictionaryProvider(config, logger);
});

builder.Services.AddSingleton<IUserAgentProvider>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var userAgentFilePath = config.GetValue<string>("UserAgent:FilePath");
    var logger = provider.GetRequiredService<ILogger<UserAgentProvider>>();
    return new UserAgentProvider(userAgentFilePath, logger);
});

builder.Services.AddHttpClient<IHttpRequester, HttpRequester>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromSeconds(2),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            MaxConnectionsPerServer = 10
        };
        return handler;
    });

builder.Services.AddSingleton<IDirectoryScanner, DirectoryScanner>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Fuzzing}/{action=Index}/{id?}");

app.Run();
