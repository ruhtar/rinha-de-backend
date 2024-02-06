#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# USER app
WORKDIR /source
COPY . .
RUN dotnet restore "./RinhaDeBackend/RinhaDeBackend.csproj" --disable-parallel
RUN dotnet publish "./RinhaDeBackend/RinhaDeBackend.csproj" -c Release -o /app --no-restore

#ARG BUILD_CONFIGURATION=Release
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "RinhaDeBackend.dll"]