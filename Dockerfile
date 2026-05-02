FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Parcial2/Parcial2.csproj Parcial2/
RUN dotnet restore Parcial2/Parcial2.csproj

COPY . .
RUN dotnet publish Parcial2/Parcial2.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Parcial2.dll"]
