# ----------------------------
# STAGE 1: Build .NET App
# ----------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy solution and project files
COPY frytech.AppleMusicTools.sln ./
COPY src/frytech.AppleMusicTools.Downloader.TelegramBot/*.csproj ./src/frytech.AppleMusicTools.Downloader.TelegramBot/

# Restore dependencies
RUN dotnet nuget add source https://pkgs.dev.azure.com/tgbots/Telegram.Bot/_packaging/release/nuget/v3/index.json -n Telegram.Bot \
    && dotnet restore -a $TARGETARCH frytech.AppleMusicTools.sln

# Copy source and publish
COPY src/ ./src/

WORKDIR /src/src/frytech.AppleMusicTools.Downloader.TelegramBot
RUN dotnet publish -c $BUILD_CONFIGURATION -a $TARGETARCH --no-restore -o /app/publish

# ----------------------------
# STAGE 2: Runtime
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Устанавливаем только необходимый Python
RUN apt-get update && apt-get install -y --no-install-recommends \
    sudo \
    python3 \
    python3-pip \
    python3-venv \
    && rm -rf /var/lib/apt/lists/* 

RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

RUN pip install --no-cache-dir gamdl

WORKDIR /app
USER app

COPY --from=build --chown=app:app /app/publish ./
COPY --from=build --chown=app:app /src/src/python/gamdl-bridge.py ./gamdl-bridge.py

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "./frytech.AppleMusicTools.Downloader.TelegramBot.dll"]