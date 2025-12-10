# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

ENV ASPNETCORE_URLS=http://+:6756
ENV ASPNETCORE_ENVIRONMENT=”production”
EXPOSE 80

#CMD [“dotnet restore" "dotnet run”]

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "FusionComms.dll"]
#CMD ASPNETCORE_URLS=http://*:$PORT dotnet StepMainApi.Core.dll

