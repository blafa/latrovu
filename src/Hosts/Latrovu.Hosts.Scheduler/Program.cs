using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Latrovu.BuildingBlocks.Infrastructure.Retention;
using Latrovu.BuildingBlocks.Infrastructure.Storage;
using Latrovu.Listings.Infrastructure;
using Latrovu.Searches.Infrastructure;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPostgres(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddEmailSending(builder.Configuration)
    .AddListingsScheduledJobs(builder.Configuration)
    .AddSearchesRetention()
    .AddMessagePublishing(builder.Configuration)
    .AddOutboxCleanup()
    .AddRetention();

IHost host = builder.Build();

IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Latrovu.Hosts.Scheduler");
lifetime.ApplicationStarted.Register(() => logger.LogInformation("Latrovu Scheduler started"));
lifetime.ApplicationStopping.Register(() => logger.LogInformation("Latrovu Scheduler stopping"));

host.Run();
