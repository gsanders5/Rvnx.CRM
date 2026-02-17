# Use dhi.io/dotnet:8-sdk for build stage as requested
FROM dhi.io/dotnet:8-sdk AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Rvnx.CRM.Web/Rvnx.CRM.Web.csproj", "Rvnx.CRM.Web/"]
COPY ["Rvnx.CRM.Core/Rvnx.CRM.Core.csproj", "Rvnx.CRM.Core/"]
COPY ["Rvnx.CRM.Infrastructure/Rvnx.CRM.Infrastructure.csproj", "Rvnx.CRM.Infrastructure/"]
COPY ["Rvnx.CRM.Shared/Rvnx.CRM.Shared.csproj", "Rvnx.CRM.Shared/"]
RUN dotnet restore "Rvnx.CRM.Web/Rvnx.CRM.Web.csproj"

# Copy the remaining source code
COPY . .
WORKDIR "/src/Rvnx.CRM.Web"

# Publish the application
RUN dotnet publish "Rvnx.CRM.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use dhi.io/aspnetcore:8 for runtime stage (web app variant)
FROM dhi.io/aspnetcore:8 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configure to listen on port 8080 (non-root user compatible)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rvnx.CRM.Web.dll"]
