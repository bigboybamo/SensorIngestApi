using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SensorIngestApi.Data;
using SensorIngestApi.Hubs;
using SensorIngestApi.Interfaces;
using SensorIngestApi.Models;
using SensorIngestApi.Services;
using Serilog;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();


// ---------- Tunables ----------
const int TargetRatePerSecond = 1000;   // simulator
const int ChannelCapacity = 50_000; // back-pressure capacity
const int Consumers = 4;      // parallel channel readers

// ---------- Services ----------
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.WriteIndented = false);

// Shared bounded channel (DI friendly)
var channel = Channel.CreateBounded<SensorReading>(new BoundedChannelOptions(ChannelCapacity)
{
    FullMode = BoundedChannelFullMode.DropOldest
});
builder.Services.AddSingleton(channel);

// Core services
builder.Services.AddSingleton<IThroughputStats>(new ThroughputStats(windowSeconds: 10));
builder.Services.AddSingleton<IAggregator, Aggregator>();
builder.Services.AddSingleton<IAlertBus, AlertBus>();

// Background workers
builder.Services.AddHostedService(sp =>
    new ChannelConsumer(
        channel: sp.GetRequiredService<Channel<SensorIngestApi.Models.SensorReading>>(),
        stats: sp.GetRequiredService<IThroughputStats>(),
        aggr: sp.GetRequiredService<IAggregator>(),
        alerts: sp.GetRequiredService<IAlertBus>(),
        hub: sp.GetRequiredService<IHubContext<TelemetryHub>>(),
        parallelConsumers: Consumers));

builder.Services.AddHostedService(sp =>
    new SensorSimulator(
        channel: sp.GetRequiredService<Channel<SensorReading>>(),
        ratePerSecond: TargetRatePerSecond,
        enabled: true)); // set false when using external load

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TelemetryDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("TelemetryDb")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<TelemetryHub>("/hub");

app.MapGet("/health", (ILoggerFactory lf, IThroughputStats stats) =>
{
    var logger = lf.CreateLogger("Health");
    var perSec = stats.GetPerSecond();

    logger.LogInformation(
        "Health ping OK: TotalProcessed={TotalProcessed} PerSecond={PerSecond} Queue={Queue}",
        stats.TotalProcessed, perSec, stats.EstimatedQueueLength);

    return Results.Ok("OK");
});

app.Run();
