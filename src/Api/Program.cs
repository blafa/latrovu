using Latrovu.Api.Dev;
using Latrovu.BuildingBlocks.Infrastructure.Ai;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.BuildingBlocks.Infrastructure.Pdf;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Latrovu.BuildingBlocks.Infrastructure.Storage;
using Latrovu.Intake.Infrastructure;
using Latrovu.Listings.Infrastructure;
using Latrovu.Listings.Infrastructure.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPostgres(builder.Configuration)
    .AddAiExtraction(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddEmailSending(builder.Configuration)
    .AddPdfReading()
    .AddListingsModule()
    .AddIntakeModule(builder.Configuration);

WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Latrovu API started"));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Latrovu API stopping"));

app.MapGet("/", () => "Latrovu API");
app.MapListingReview();
if (app.Environment.IsDevelopment())
{
    app.MapDevImport();
}

app.Run();
