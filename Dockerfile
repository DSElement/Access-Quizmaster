FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AccessRag.csproj ./
RUN dotnet restore AccessRag.csproj

COPY . .
RUN dotnet publish AccessRag.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /out ./

ENTRYPOINT ["dotnet", "AccessRag.dll"]
