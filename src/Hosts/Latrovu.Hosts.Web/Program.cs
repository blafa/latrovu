using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Latrovu.BuildingBlocks.Infrastructure.Storage;
using Latrovu.Listings.Infrastructure;
using Latrovu.Listings.Infrastructure.Review.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPostgres(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddEmailSending(builder.Configuration)
    .AddListingsReview()
    .AddMessagePublishing(builder.Configuration);

WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Latrovu Web started"));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Latrovu Web stopping"));

app.MapGet("/", () => "Latrovu Web");
app.MapListingReview();

app.Run();
