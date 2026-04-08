echo "Database is available - starting the app"
#!/bin/bash
set -e

host="${DB_HOST:-db}"
port="${DB_PORT:-1433}"

echo "Waiting for $host:$port..."

# Wait until the TCP port is open
while ! bash -c "</dev/tcp/$host/$port" >/dev/null 2>&1; do
  sleep 1
done

echo "Database is available - starting the app"
exec dotnet project1.dll
