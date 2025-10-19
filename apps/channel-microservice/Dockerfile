# --- Runtime base ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# --- Build/restore ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# copy csproj first (cache-friendly)
COPY apps/channel-microservice/src/ChannelMicroservice.csproj apps/channel-microservice/src/
RUN dotnet restore apps/channel-microservice/src/ChannelMicroservice.csproj
# copy the rest of the repo
COPY . .
WORKDIR /src/apps/channel-microservice/src
RUN dotnet build -c Release -o /app/build

# --- Publish ---
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# --- Final image ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChannelMicroservice.dll"]
