FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY ACE.MarketApi.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5271
EXPOSE 5271
ENTRYPOINT ["dotnet", "ACE.MarketApi.dll"]
