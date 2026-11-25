using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Latrovu.BuildingBlocks.Infrastructure.Storage;
using Latrovu.Core.Listings;
using Latrovu.Hosts.Intake.Dev;
using Latrovu.Intake.Infrastructure;

const string InputQueue = "latrovu-intake";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPostgres(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddIntakeModule(builder.Configuration)
    .AddMessageConsuming(builder.Configuration, InputQueue, [typeof(ListingWithdrawn)]);

WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Latrovu Intake started"));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Latrovu Intake stopping"));

app.MapGet("/", () => "Latrovu Intake");
if (app.Environment.IsDevelopment())
{
    app.MapDevImport();
    app.MapDevSearch();
    app.MapDevMail();
}

app.Run();
