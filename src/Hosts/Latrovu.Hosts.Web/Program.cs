using Latrovu.BuildingBlocks.Infrastructure.Ai;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Latrovu.BuildingBlocks.Infrastructure.Storage;
using Latrovu.Hosts.Web.Dev;
using Latrovu.Listings.Infrastructure;
using Latrovu.Listings.Infrastructure.Review.Web;
using Latrovu.Listings.Infrastructure.Viewing.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPostgres(builder.Configuration)
    .AddAiExtraction(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddEmailSending(builder.Configuration)
    .AddListingsReview()
    .AddListingsViewing()
    .AddMessagePublishing(builder.Configuration);

WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Latrovu Web started"));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Latrovu Web stopping"));

app.UseStaticFiles();
app.UseRateLimiter();
app.MapGet("/", () => "Latrovu Web");
app.MapListingReview();
app.MapListingViews();
if (app.Environment.IsDevelopment())
{
    app.MapDevViewLinks();
}

app.Run();
