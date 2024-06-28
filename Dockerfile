# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /source

# Install Node.js
RUN apt-get -y update &&\
  apt-get install -y curl &&\
  curl -sL https://deb.nodesource.com/setup_22.x | bash - &&\
  apt-get install -y nodejs &&\
  apt-get clean

# Install Node.js dependencies
COPY package*.json .
RUN npm install

# Copy source files, binaries, and nx workspace file
COPY ./ .

# Publish to /ace
RUN npx nx run server:publish:docker

# Final Stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy
ARG DEBIAN_FRONTEND="noninteractive"
WORKDIR /ace

# Install net-tools (netstat for health check) & cleanup
RUN apt-get update && \
  apt-get install --no-install-recommends -y \
  net-tools && \
  apt-get clean && \
  rm -rf \
  /tmp/* \
  /var/lib/apt/lists/* \
  /var/tmp/*

# Add app from build
COPY --from=build /ace .

# Run app
ENTRYPOINT ["dotnet", "ACE.Server.dll"]

# Expose ports and volumes
EXPOSE 9000-9001/udp
VOLUME /ace/Config /ace/Content /ace/Dats /ace/Logs /ace/Mods

# Health check
HEALTHCHECK --start-period=5m --interval=1m --timeout=3s \
  CMD netstat -an | grep 9000 > /dev/null; if [ 0 != $? ]; then exit 1; fi;
