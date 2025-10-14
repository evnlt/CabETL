## Docker setup for Windows

1. Setup `MS SQL Server`
    > `docker run --name mssql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Qwerty123$%" -p 1433:1433 -d -v d:\db-data\mssql:/var/opt/mssql/data --restart unless-stopped mcr.microsoft.com/mssql/server:2022-latest`