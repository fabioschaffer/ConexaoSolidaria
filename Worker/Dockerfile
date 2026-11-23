FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Application/ Application/
COPY Domain/ Domain/
COPY Infra/ Infra/
COPY Worker/ Worker/
RUN dotnet publish Worker/Worker.csproj --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Worker.dll"]
