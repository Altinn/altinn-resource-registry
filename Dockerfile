FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:f271ed7d0fd9c5a7ed0acafed8a2bc978bb65c19dcd2eeea0415adef142ffc87 AS build
WORKDIR /app

# Copy everything and build
COPY . .
RUN dotnet publish -c Release -o out ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj \
  && cp -r src/Altinn.ResourceRegistry.Persistence/Migration out/Migration

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:bbec28d980bc555bf823296a63f161ea6c6aeba9acbc452eb03e6e7a57ca5f0c AS final
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
