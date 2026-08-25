FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SeniorDotNetPractice.Api.csproj", "./"]

RUN dotnet restore "SeniorDotNetPractice.Api.csproj"

COPY . .

RUN dotnet publish "SeniorDotNetPractice.Api.csproj" -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SeniorDotNetPractice.Api.dll"]