FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:bc43a6c308396eb1364b293da5315a55f5f8921ffd08ed61b227e5b35e88e237 AS build
WORKDIR /app

# Copy everything and build
COPY . .
RUN dotnet publish -c Release -o out ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj \
  && cp -r src/Altinn.ResourceRegistry.Persistence/Migration out/Migration

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:5e8dca92553951e42caed00f2568771b0620679f419a28b1335da366477d7f98 AS final
EXPOSE 5100
WORKDIR /app

COPY --from=build /app/out .

# setup the user and group
# the user will have no password, using shell /bin/false and using the group dotnet
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet
# update permissions of files if neccessary before becoming dotnet user
USER dotnet
RUN mkdir /tmp/logtelemetry
ENTRYPOINT ["dotnet", "Altinn.ResourceRegistry.dll"]
