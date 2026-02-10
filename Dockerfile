FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:e0f7abfb9c18aa419198b2c4e05dc2f7fd864a6098f09bddb1ceb2b0bce67685 AS build
WORKDIR /app

# Copy everything and build
COPY . .
RUN dotnet publish -c Release -o out ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj \
  && cp -r src/Altinn.ResourceRegistry.Persistence/Migration out/Migration

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:c60d76bf35b268f7bab7d5f4e5405c89dcc45616453004e89baea303677ca635 AS final
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
