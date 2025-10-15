using FleetManagement.API.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Debugging;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http; // <-- FIX 2: REQUIRED for HttpClientHandler/SSL Bypass
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
//minimal config ot load secrests

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
//Now retrieve the secrest from secrests.json
var splunkHost = configuration["Splunk:Host"];
var eventCollectorToken = configuration["Splunk:Token"];
var applicationInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");

// FIX 1: IMPLEMENT SSL BYPASS
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/fleet-management-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.EventCollector(
        splunkHost: splunkHost, 
        eventCollectorToken: eventCollectorToken,
        // Inject the custom handler to bypass SSL check (equivalent of 'curl -k')
        messageHandler: new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (
                HttpRequestMessage msg,
                X509Certificate2? cert,
                X509Chain? chain,
                System.Net.Security.SslPolicyErrors errors) => true
        })
    .CreateLogger();
    

try
{
    Log.Information("Starting Fleet Management API");

    //var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsConnectionString;
        //options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        // OR use InstrumentationKey: options.InstrumentationKey = "YOUR-INSTRUMENTATION-KEY-HERE";
    });
    // Add services
    builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add DbContext
    if (builder.Environment.IsProduction())
    {
        builder.Services.AddDbContext<FleetContext>(options =>
            options.UseInMemoryDatabase("FleetManagement"));
    }
    else
    {
        builder.Services.AddDbContext<FleetContext>(options =>
            options.UseSqlServer(defaultConnectionString));
    }
    //builder.Services.AddDbContext<FleetContext>(options =>
    //    options.UseSqlServer(defaultConnectionString));
    //    //options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    var app = builder.Build();


    // Configure HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    
    // FIX 3: Routing Order Corrected
    // 1. Map controllers (API and Swagger)
    app.MapControllers();
    
    // 2. Serve static files (frontend) and use fallback
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");


    // Seed the database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<FleetContext>();
        DbInitializer.Initialize(context);
        Log.Information("Database seeding completed");
    }

    Log.Information("Application configured successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}



//using FleetManagement.API.Data;
//using Microsoft.EntityFrameworkCore;
//using Serilog;
//using Serilog.Debugging;
//using System.Security.Cryptography.X509Certificates;
//using System.Net.Http; // <-- FIX 2: REQUIRED for HttpClientHandler/SSL Bypass
//using Microsoft.Extensions.Configuration;

//// FIX 1: IMPLEMENT SSL BYPASS
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
//    .Enrich.FromLogContext()
//    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
//    .WriteTo.File(
//        path: "logs/fleet-management-.log",
//        rollingInterval: RollingInterval.Day,
//        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
//    .WriteTo.EventCollector(
//        splunkHost: "realaddresshere",
//        eventCollectorToken: "realtoken here",
//        // Inject the custom handler to bypass SSL check (equivalent of 'curl -k')
//        messageHandler: new HttpClientHandler
//        {
//            ServerCertificateCustomValidationCallback = (
//                HttpRequestMessage msg,
//                X509Certificate2? cert,
//                X509Chain? chain,
//                System.Net.Security.SslPolicyErrors errors) => true
//        })
//    .CreateLogger();


//try
//{
//    Log.Information("Starting Fleet Management API");

//    var builder = WebApplication.CreateBuilder(args);

//    // Add Serilog
//    builder.Host.UseSerilog();
//    builder.Services.AddApplicationInsightsTelemetry(options =>
//    {
//        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
//        // OR use InstrumentationKey: options.InstrumentationKey = "YOUR-INSTRUMENTATION-KEY-HERE";
//    });
//    // Add services
//    builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
//    });

//    builder.Services.AddEndpointsApiExplorer();
//    builder.Services.AddSwaggerGen();

//    // Add DbContext
//    builder.Services.AddDbContext<FleetContext>(options =>
//        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//    var app = builder.Build();


//    // Configure HTTP request pipeline
//    if (app.Environment.IsDevelopment())
//    {
//        app.UseSwagger();
//        app.UseSwaggerUI();
//    }

//    app.UseSerilogRequestLogging();

//    app.UseHttpsRedirection();
//    app.UseAuthorization();

//    // FIX 3: Routing Order Corrected
//    // 1. Map controllers (API and Swagger)
//    app.MapControllers();

//    // 2. Serve static files (frontend) and use fallback
//    app.UseStaticFiles();
//    app.MapFallbackToFile("index.html");


//    // Seed the database
//    using (var scope = app.Services.CreateScope())
//    {
//        var services = scope.ServiceProvider;
//        var context = services.GetRequiredService<FleetContext>();
//        DbInitializer.Initialize(context);
//        Log.Information("Database seeding completed");
//    }

//    Log.Information("Application configured successfully");
//    app.Run();
//}
//catch (Exception ex)
//{
//    Log.Fatal(ex, "Application terminated unexpectedly");
//}
//finally
//{
//    Log.CloseAndFlush();
//}





























//using FleetManagement.API.Data;
//using Microsoft.EntityFrameworkCore;
//using Serilog;
//using Serilog.Debugging;
//using System.Security.Cryptography.X509Certificates;
//using System.Net.Http;


//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
//    .Enrich.FromLogContext()
//    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
//    .WriteTo.File(
//        path: "logs/fleet-management-.log",
//        rollingInterval: RollingInterval.Day,
//        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
//    //.WriteTo.EventCollector(
//    //    splunkHost: "https://prd-p-2ek8j.splunkcloud.com:8088", ///services/collector/event",
//    //    //e46576c9-ed3f-4712-a1bf-26d6fc9d6dbc fro test2
//    //    eventCollectorToken: "8a45f64d-f2c7-4683-b690-bd51eb5bbc6c")
//    .WriteTo.EventCollector(
//        splunkHost: "https://prd-p-2ek8j.splunkcloud.com:8088", // Corrected to base URL (safer)
//        eventCollectorToken: "8a45f64d-f2c7-4683-b690-bd51eb5bbc6c",
//        // Inject the custom handler to bypass SSL check (equivalent of 'curl -k')
//        messageHandler: new HttpClientHandler
//        {
//            ServerCertificateCustomValidationCallback = (
//                HttpRequestMessage msg,
//                X509Certificate2? cert,
//                X509Chain? chain,
//                System.Net.Security.SslPolicyErrors errors) => true
//        })
//    .CreateLogger();


//try
//{
//    Log.Information("Starting Fleet Management API");

//    var builder = WebApplication.CreateBuilder(args);

//    // Add Serilog
//    builder.Host.UseSerilog();

//    // Add services
//    //builder.Services.AddControllers();
//    builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
//    });

//    builder.Services.AddEndpointsApiExplorer();
//    builder.Services.AddSwaggerGen();

//    // Add DbContext
//    builder.Services.AddDbContext<FleetContext>(options =>
//        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//    var app = builder.Build();


//    //app.UseDefaultFiles();
//    // Serve static files from wwwroot
//    app.UseStaticFiles();
//    app.MapFallbackToFile("index.html");

//    // Configure HTTP request pipeline
//    if (app.Environment.IsDevelopment())
//    {
//        app.UseSwagger();
//        app.UseSwaggerUI();
//    }

//    app.UseSerilogRequestLogging();

//    app.UseHttpsRedirection();
//    app.UseAuthorization();
//    app.MapControllers();

//    // Seed the database
//    using (var scope = app.Services.CreateScope())
//    {
//        var services = scope.ServiceProvider;
//        var context = services.GetRequiredService<FleetContext>();
//        DbInitializer.Initialize(context);
//        Log.Information("Database seeding completed");
//    }

//    Log.Information("Application configured successfully");
//    app.Run();
//}
//catch (Exception ex)
//{
//    Log.Fatal(ex, "Application terminated unexpectedly");
//}
//finally
//{
//    Log.CloseAndFlush();
//}


