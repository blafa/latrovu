using Latrovu.BuildingBlocks.Infrastructure.Ai;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.BuildingBlocks.Infrastructure.Geo;
using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.BuildingBlocks.Infrastructure.Pdf;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Latrovu.BuildingBlocks.Infrastructure.Storage;
using Latrovu.Core.Listings;
using Latrovu.Core.Searches;
using Latrovu.Core.Submissions;
using Latrovu.Listings.Infrastructure;
using Latrovu.Searches.Infrastructure;

const string InputQueue = "latrovu-extraction";

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPostgres(builder.Configuration)
    .AddAiExtraction(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddEmailSending(builder.Configuration)
    .AddGeocoding()
    .AddPdfReading()
    .AddListingsProcessing(builder.Configuration)
    .AddSearchesModule(builder.Configuration)
    .AddMessageConsuming(
        builder.Configuration,
        InputQueue,
        [typeof(SubmissionReceived), typeof(SearchRequestReceived), typeof(ListingApproved), typeof(ListingReadyForMatching), typeof(ViewLinkRequested), typeof(ListingWithdrawn)]);

IHost host = builder.Build();
host.Run();
