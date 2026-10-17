FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Horse_BackEnd/Horse_BackEnd.csproj Horse_BackEnd/
COPY HorseClub.BLL/HorseClub.BLL.csproj HorseClub.BLL/
COPY HorseClub.DAL/HorseClub.DAL.csproj HorseClub.DAL/
RUN dotnet restore Horse_BackEnd/Horse_BackEnd.csproj
COPY Horse_BackEnd/ Horse_BackEnd/
COPY HorseClub.BLL/ HorseClub.BLL/
COPY HorseClub.DAL/ HorseClub.DAL/
COPY docs/data/public-racing-reference-2026-10-13.json docs/data/
RUN dotnet publish Horse_BackEnd/Horse_BackEnd.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Horse_BackEnd.dll"]
