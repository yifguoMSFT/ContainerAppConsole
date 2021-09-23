#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 10080


FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["ConsoleApi.csproj", "."]
RUN dotnet restore "./ConsoleApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ConsoleApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsoleApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleApi.dll"]