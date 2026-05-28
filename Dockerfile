# ──────────────────────────────────────────────
# Hearth.AI demo image
# Exposes the OpenAI-compatible /v1 API on port 5000.
#
# Usage:
#   docker build -t hearth-ai/demo .
#   docker run -p 5000:5000 \
#     -v /path/to/your/models:/app/models \
#     -e HEARTH_MODEL=/app/models/qwen2.5-7b-instruct-q4_k_m.gguf \
#     hearth-ai/demo
#
# Then hit it like any OpenAI endpoint:
#   curl http://localhost:5000/v1/models
# ──────────────────────────────────────────────

# ── Build stage ──────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ ./src/
COPY samples/ ./samples/

WORKDIR /src/samples/Hearth.Samples.Blazor
RUN dotnet publish -c Release -o /app/publish \
    --no-self-contained \
    -p:PublishSingleFile=false

# ── Runtime stage ─────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Model volume mount point — users bind-mount their own GGUF files here.
VOLUME ["/app/models"]

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV HEARTH_MODEL=/app/models/model.gguf

EXPOSE 5000

ENTRYPOINT ["dotnet", "Hearth.Samples.Blazor.dll"]
