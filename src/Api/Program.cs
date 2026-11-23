using Latrovu.Api.Dev;
using Latrovu.Api.Mail;
using Latrovu.Core;
using Latrovu.Infrastructure.Ai;
using Latrovu.Infrastructure.Mail;
using Latrovu.Infrastructure.Pdf;
using Latrovu.Infrastructure.Persistence;
using Latrovu.Infrastructure.Storage;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCore()
    .AddMailInbox(builder.Configuration)
    .AddAiExtraction(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddPdfReading()
    .AddPersistence(builder.Configuration);
builder.Services.AddHostedService<MailPollingService>();

WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Latrovu API started"));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Latrovu API stopping"));

await app.Services.GetRequiredService<DatabaseSchema>().EnsureCreatedAsync();

app.MapGet("/", () => "Latrovu API");
if (app.Environment.IsDevelopment())
{
    app.MapDevImport();
}

app.Run();
