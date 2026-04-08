# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["project1.csproj", "./"]
RUN dotnet restore "project1.csproj"

# Copy everything and publish
COPY . .
RUN dotnet publish "project1.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# copy entrypoint and make executable
COPY entrypoint.sh ./
RUN chmod +x ./entrypoint.sh
RUN sed -i 's/\r$//' ./entrypoint.sh

ENV ASPNETCORE_URLS="http://+:80"
EXPOSE 80

ENTRYPOINT ["/bin/bash", "./entrypoint.sh"]
