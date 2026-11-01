FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition files
COPY ["WorkflowEngine.API/WorkflowEngine.API.csproj", "WorkflowEngine.API/"]
COPY ["WorkflowEngine.Application/WorkflowEngine.Application.csproj", "WorkflowEngine.Application/"]
COPY ["WorkflowEngine.Domain/WorkflowEngine.Domain.csproj", "WorkflowEngine.Domain/"]
COPY ["WorkflowEngine.Infrastructure/WorkflowEngine.Infrastructure.csproj", "WorkflowEngine.Infrastructure/"]

# Restore packages
RUN dotnet restore "WorkflowEngine.API/WorkflowEngine.API.csproj"

# Copy remaining code files and build
COPY . .
WORKDIR "/src/WorkflowEngine.API"
RUN dotnet build "WorkflowEngine.API.csproj" -c Release -o /app/build
RUN dotnet publish "WorkflowEngine.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Package output image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "WorkflowEngine.API.dll"]
