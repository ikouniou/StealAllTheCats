# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
# For more information, please see https://aka.ms/containercompat

# This stage is used when running from VS in fast mode (Default for Debug configuration)
# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the whole repo (fast enough with a .dockerignore)
COPY . .

# Restore & publish the Web API (it references ORM/LibCat so they come along)
WORKDIR /src/StealAllTheCatsWebApi
RUN dotnet restore
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /out ./
ENTRYPOINT ["dotnet","StealAllTheCatsWebApi.dll"]