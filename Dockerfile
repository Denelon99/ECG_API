# ---- build stage: SDK image, compiles and publishes the API ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, in its own layer: it only reruns when the csproj changes
COPY ECG_API/ECG_API.csproj ECG_API/
RUN dotnet restore ECG_API/ECG_API.csproj -r linux-x64

COPY ECG_API/ ECG_API/
RUN dotnet publish ECG_API/ECG_API.csproj -c Release -r linux-x64 --self-contained false \
    --no-restore -o /app/publish

# ---- runtime stage: ASP.NET runtime only, no SDK/compiler ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# lib_lightgbm.so (ML.NET LightGBM) needs OpenMP; the base image doesn't ship it
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Writable folders for the volumes, owned by the non-root "app" user from the base image
RUN mkdir -p /app/db /app/ProcessedImages /app/Core/MLModels \
    && chown -R $APP_UID /app/db /app/ProcessedImages

COPY --from=build /app/publish .

ENV ConnectionStrings__EcgDb="Data Source=/app/db/ecg.db"
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "ECG_API.dll"]
