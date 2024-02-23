# First stage: Playwright installation
FROM mcr.microsoft.com/playwright:v1.38.1-focal as build
WORKDIR /app
EXPOSE 80
EXPOSE 443
# Install .NET

RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-8.0

# Copy csproj and restore as distinct layers
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VisaIta/VisaIta.csproj", "VisaIta/"]
RUN dotnet restore "./VisaIta/./VisaIta.csproj"
COPY . .
WORKDIR "/src/VisaIta"
RUN dotnet build "./VisaIta.csproj" -c $BUILD_CONFIGURATION -o /app/out

ENV ASPNETCORE_URLS=http://+:80
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright 

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

RUN dotnet dev-certs https --trust

# Second stage: Runtime
FROM mcr.microsoft.com/playwright:v1.38.1-focal
WORKDIR /app

# Install .NET runtime
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y aspnetcore-runtime-8.0

# Copy build output from the build stage
COPY --from=build /app/out ./

RUN sh /app/.playwright/node/linux-x64/playwright.sh install chromium
RUN sh /app/.playwright/node/linux-x64/playwright.sh install-deps chromium


# Run the .NET application
ENTRYPOINT ["dotnet", "VisaIta.dll"]