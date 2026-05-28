FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Hearth/Hearth.csproj src/Hearth/
COPY src/Hearth.AspNetCore/Hearth.AspNetCore.csproj src/Hearth.AspNetCore/
COPY samples/Hearth.Samples.Server/Hearth.Samples.Server.csproj samples/Hearth.Samples.Server/

RUN dotnet restore samples/Hearth.Samples.Server/Hearth.Samples.Server.csproj

COPY src/ src/
COPY samples/Hearth.Samples.Server/ samples/Hearth.Samples.Server/

RUN dotnet publish samples/Hearth.Samples.Server/Hearth.Samples.Server.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgomp1 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/models

ENV ASPNETCORE_URLS=http://+:5000
ENV HEARTH__CONTEXTSIZE=4096
ENV HEARTH__BATCHSIZE=512
ENV HEARTH__GPULAYERS=0

EXPOSE 5000

ENTRYPOINT ["dotnet", "Hearth.Samples.Server.dll"]
