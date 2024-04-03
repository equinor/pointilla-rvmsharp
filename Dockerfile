# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ./RvmFilter/RvmFilter.csproj ./
RUN dotnet restore
# copy everything
COPY . .

WORKDIR /RvmFilter
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
#RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

EXPOSE 8000

# Runtime user change to non-root for added security
RUN useradd -ms /bin/bash --uid 1001 isar
RUN chown -R 1001 /app
RUN chmod 755 /app
USER 1001

ENTRYPOINT ["dotnet", "RvmFilter.dll", "--urls=http://0.0.0.0:8000"]
