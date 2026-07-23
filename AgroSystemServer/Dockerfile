FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY . .

RUN dotnet restore AgroSystemServer.csproj
RUN dotnet publish AgroSystemServer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview

WORKDIR /app

COPY --from=build /app/publish .

# Crear la carpeta que está pidiendo .NET al iniciar
RUN mkdir -p /app/Imagenes

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet","AgroSystemServer.dll"]