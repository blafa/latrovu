#!/usr/bin/env bash
set -euo pipefail
cd /mnt/c/dev/latrovu

dotnet sln remove tests/Latrovu.Api.Tests/Latrovu.Api.Tests.csproj

mkdir -p src/Core/{Ai,Images,Listings,Mail,Pdf,Storage} src/Infrastructure/{Ai,Mail,Pdf,Storage,Persistence}

mv src/Api/Ai/ILlmClient.cs src/Api/Ai/JsonResponseSchema.cs src/Api/Ai/LlmJsonNormalizer.cs src/Core/Ai/
mv src/Api/Images/ImageFile.cs src/Core/Images/
mv src/Api/Listings/*.cs src/Core/Listings/
mv src/Api/Data/IListingStore.cs src/Api/Data/IListingImageStore.cs src/Core/Listings/
mv src/Api/Mail/IMailDispatcher.cs src/Api/Mail/IMailInbox.cs src/Api/Mail/IMailRouter.cs src/Api/Mail/InboundMail.cs \
   src/Api/Mail/MailAttachment.cs src/Api/Mail/MailDispatcher.cs src/Api/Mail/MailKind.cs src/Api/Mail/MailRouter.cs src/Core/Mail/
mv src/Api/Pdf/IPdfImageReader.cs src/Api/Pdf/IPdfTextReader.cs src/Core/Pdf/
mv src/Api/Storage/IObjectStorage.cs src/Core/Storage/

mv src/Api/Ai/*.cs src/Infrastructure/Ai/
mv src/Api/Mail/ImapInbox.cs src/Api/Mail/MailOptions.cs src/Infrastructure/Mail/
mv src/Api/Pdf/*.cs src/Infrastructure/Pdf/
mv src/Api/Storage/*.cs src/Infrastructure/Storage/
mv src/Api/Data/*.cs src/Infrastructure/Persistence/
rmdir src/Api/Ai src/Api/Images src/Api/Listings src/Api/Pdf src/Api/Storage src/Api/Data

find src/Core -name '*.cs' -exec sed -i -E \
  's/Latrovu\.Api\.Data/Latrovu.Core.Listings/g; s/Latrovu\.Api\.(Ai|Images|Listings|Mail|Pdf|Storage)/Latrovu.Core.\1/g' {} +
sed -i '/^using Latrovu\.Core\.Listings;$/d' src/Core/Listings/*.cs

find src/Infrastructure -name '*.cs' -exec sed -i -E \
  's/namespace Latrovu\.Api\.Data;/namespace Latrovu.Infrastructure.Persistence;/; s/namespace Latrovu\.Api\.(Ai|Mail|Pdf|Storage);/namespace Latrovu.Infrastructure.\1;/; s/using Latrovu\.Api\.(Ai|Images|Listings|Mail|Pdf|Storage);/using Latrovu.Core.\1;/' {} +
sed -i '1i using Latrovu.Core.Ai;' src/Infrastructure/Ai/InfomaniakLlmClient.cs
sed -i '1i using Latrovu.Core.Mail;' src/Infrastructure/Mail/ImapInbox.cs
sed -i '1i using Latrovu.Core.Pdf;' src/Infrastructure/Pdf/PdfPigImageReader.cs src/Infrastructure/Pdf/PdfPigTextReader.cs
sed -i '1i using Latrovu.Core.Storage;' src/Infrastructure/Storage/S3ObjectStorage.cs
sed -i '1i using Latrovu.Core.Listings;' src/Infrastructure/Persistence/ListingImageStore.cs

sed -i 's/using Latrovu\.Api\.Mail;/using Latrovu.Core.Mail;/' src/Api/Dev/DevImportEndpoint.cs
sed -i '1i using Latrovu.Infrastructure.Mail;' src/Api/Mail/MailPollingService.cs
sed -i '1i using Latrovu.Core.Mail;' src/Api/Mail/MailPollingService.cs

rm -rf tests/Latrovu.Api.Tests/bin tests/Latrovu.Api.Tests/obj
mv tests/Latrovu.Api.Tests tests/Latrovu.Core.Tests
mv tests/Latrovu.Core.Tests/Latrovu.Api.Tests.csproj tests/Latrovu.Core.Tests/Latrovu.Core.Tests.csproj
sed -i -E 's#src[\\/]Api[\\/]Latrovu\.Api\.csproj#src\\Core\\Latrovu.Core.csproj#' tests/Latrovu.Core.Tests/Latrovu.Core.Tests.csproj
find tests/Latrovu.Core.Tests -name '*.cs' -exec sed -i -E \
  's/Latrovu\.Api\.Tests/Latrovu.Core.Tests/g; s/Latrovu\.Api\.(Ai|Images|Listings|Mail)/Latrovu.Core.\1/g' {} +

dotnet new xunit -o tests/Latrovu.Infrastructure.Tests
rm -f tests/Latrovu.Infrastructure.Tests/UnitTest1.cs
mv tests/Latrovu.Core.Tests/SigV4SignerTests.cs tests/Latrovu.Infrastructure.Tests/
sed -i -E 's/Latrovu\.Core\.Tests/Latrovu.Infrastructure.Tests/; s/Latrovu\.Api\.Storage/Latrovu.Infrastructure.Storage/' \
  tests/Latrovu.Infrastructure.Tests/SigV4SignerTests.cs
dotnet add tests/Latrovu.Infrastructure.Tests reference src/Infrastructure

dotnet sln add src/Core src/Infrastructure tests/Latrovu.Core.Tests tests/Latrovu.Infrastructure.Tests

echo "Restructuring done"
