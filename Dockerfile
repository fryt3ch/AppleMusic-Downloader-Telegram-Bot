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
RUN dotnet publish -o /app -c $BUILD_CONFIGURATION -a $TARGETARCH --no-restore

# ----------------------------
# STAGE 2: Build tools
# ----------------------------
FROM --platform=$BUILDPLATFORM debian:bookworm-slim AS tools-build

ARG TARGETARCH

WORKDIR /tmp

# Install build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    bash \
    curl \
    git \
    unzip \
    build-essential \
    cmake \
    python3 \
    perl \
    autoconf \
    automake \
    libtool \
    nasm \
    zlib1g-dev \
    pkg-config \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Build and install Bento4 (all tools including mp4decrypt)
RUN git clone https://github.com/axiomatic-systems/Bento4.git bento4 \
    && cd bento4/Build \
    && cmake -DCMAKE_BUILD_TYPE=Release .. \
    && make -j$(nproc) \
    && make install \
    && cd /tmp && rm -rf bento4

# ----------------------------
# STAGE 3: Runtime
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install sudo
RUN apt-get update && apt-get install -y --no-install-recommends \
    sudo \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user 'appuser' with UID 1000, home directory, and sudo privileges
RUN useradd -m -u 1000 -s /bin/bash appuser \
    && echo "appuser ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/appuser \
    && chmod 0440 /etc/sudoers.d/appuser

# Set working directory
WORKDIR /app

# Copy built tools and .NET app
COPY --from=tools-build /usr/local/bin/ /usr/local/bin/
COPY --from=build /app ./

# Set environment and entrypoint
ENV ASPNETCORE_ENVIRONMENT=Production

# Switch to the non-root user
USER appuser

EXPOSE 8080
ENTRYPOINT ["./frytech.AppleMusicTools.Downloader.TelegramBot"]
