FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5122

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["src/ClientService.Api/ClientService.Api.csproj", "src/ClientService.Api/"]
COPY ["src/ClientService.Application/ClientService.Application.csproj", "src/ClientService.Application/"]
COPY ["src/ClientService.Domain/ClientService.Domain.csproj", "src/ClientService.Domain/"]
COPY ["src/ClientService.Infra/ClientService.Infra.csproj", "src/ClientService.Infra/"]

# Restaurar dependências
RUN dotnet restore "src/ClientService.Api/ClientService.Api.csproj"

# Copiar código fonte
COPY . .

# Build da aplicação
WORKDIR "/src/src/ClientService.Api"
RUN dotnet build "ClientService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClientService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClientService.Api.dll"]