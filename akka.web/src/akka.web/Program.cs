using System.Diagnostics;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using akka.web;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders().AddConsole();

var akkaConfig = builder.Configuration.GetRequiredSection(nameof(AkkaClusterConfig))
    .Get<AkkaClusterConfig>();

builder.Services.AddControllers();
builder.Services.AddAkka(akkaConfig.ActorSystemName, (builder, provider) =>
{
    Debug.Assert(akkaConfig.Port != null, "akkaConfig.Port != null");
    builder.AddHoconFile("app.conf", HoconAddMode.Append)
        .WithRemoting(akkaConfig.Hostname, akkaConfig.Port.Value)
        .WithClustering(new ClusterOptions()
        {
            Roles = akkaConfig.Roles,
            SeedNodes = akkaConfig.SeedNodes
        })
        .WithActors((system, registry) =>
        {
            var consoleActor = system.ActorOf(Props.Create(() => new ConsoleActor()), "console");
            registry.Register<ConsoleActor>(consoleActor);
        });
});

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();

    endpoints.MapGet("/", async (HttpContext context, ActorRegistry registry) =>
    {
        var reporter = registry.Get<ConsoleActor>();
        var resp = await reporter.Ask<string>($"hit from {context.TraceIdentifier}", context.RequestAborted); // calls Akka.NET under the covers
        await context.Response.WriteAsync(resp);
    });
});

await app.RunAsync();