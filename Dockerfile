FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:2303ad5956875eb82d3c6195e43f0e8e1378a6252869f2d4d200e067130ff5b5 AS build
WORKDIR /app

# Copy everything and build
COPY . .
RUN dotnet publish -c Release -o out ./src/Altinn.ResourceRegistry/Altinn.ResourceRegistry.csproj \
  && cp -r src/Altinn.ResourceRegistry.Persistence/Migration out/Migration

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:374a0ebc32ae59692470070a8bbcdef1186250d446836bf6ec8ac08a5c623667 AS final
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
