# Runs the trainer without a local .NET SDK.
#   docker build -t security-trainer .
#   docker run --rm -p 127.0.0.1:5000:5000 security-trainer
# Keep the 127.0.0.1 prefix: this app is DELIBERATELY VULNERABLE and must not be
# reachable from the network.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Trainer.Web/Trainer.Web.csproj src/Trainer.Web/
RUN dotnet restore src/Trainer.Web/Trainer.Web.csproj
COPY src/Trainer.Web/ src/Trainer.Web/
RUN dotnet publish src/Trainer.Web/Trainer.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Inside the container the app must listen on all interfaces so the published
# port can reach it. Exposure is limited by `-p 127.0.0.1:...` on the host.
ENV Kestrel__Endpoints__Http__Url=http://0.0.0.0:5000
EXPOSE 5000
USER $APP_UID
ENTRYPOINT ["dotnet", "Trainer.Web.dll"]
