#!/bin/bash
# Wait for SQL Server to start
echo "Waiting for SQL Server to start..."
sleep 30s

# Run the SQL script to create the database
echo "Running database initialization script..."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -d master -i /create-database.sql

# Keep the container running
exec /opt/mssql/bin/sqlservr
