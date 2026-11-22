using System.Net.Http.Headers;
using Latrovu.Api.Ai;
using Latrovu.Api.Data;
using Latrovu.Api.Dev;
using Latrovu.Api.Listings;
using Latrovu.Api.Mail;
using Latrovu.Api.Pdf;
using Microsoft.Extensions.Options;
using Npgsql;

const string DatabaseConnectionName = "Db";
const string BearerScheme = "Bearer";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<MailOptions>()
    .Bind(builder.Configuration.GetSection(MailOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<AiOptions>()
    .Bind(builder.Configuration.GetSection(AiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

string connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
    ?? throw new InvalidOperationException($"ConnectionStrings:{DatabaseConnectionName} is missing");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<DatabaseSchema>();
builder.Services.AddSingleton<IListingStore, ListingStore>();
builder.Services.AddSingleton<IMailInbox, ImapInbox>();
builder.Services.AddSingleton<IMailRouter, MailRouter>();
builder.Services.AddSingleton<IPdfTextReader, PdfPigTextReader>();
builder.Services.AddScoped<IDocumentClassifier, DocumentClassifier>();
builder.Services.AddScoped<IListingExtractor, ListingExtractor>();
builder.Services.AddScoped<IListingIntake, ListingIntake>();
builder.Services.AddScoped<IMailDispatcher, MailDispatcher>();
builder.Services.AddHttpClient<ILlmClient, InfomaniakLlmClient>(ConfigureLlmHttpClient);
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

static void ConfigureLlmHttpClient(IServiceProvider services, HttpClient http)
{
    AiOptions ai = services.GetRequiredService<IOptions<AiOptions>>().Value;
    http.BaseAddress = new Uri(ai.BaseUrl);
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, ai.Token);
}
