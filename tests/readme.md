# Docker Containers

## AWS

docker run -d --rm -it -p 4566:4566 -p 4571:4571 -e SERVICES=lambda,s3,secretsmanager,dynamodb -e LAMBDA_RUNTIME_ENVIRONMENT_TIMEOUT=120 -e DEBUG=1 -v /var/run/docker.sock:/var/run/docker.sock localstack/localstack


## SQL Server

```bash
# Microsoft SQL Server
docker run -d --rm -it -p 1453:1433 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=nc_Test_Pipeline!" mcr.microsoft.com/mssql/server:2022-latest
# Microsoft SQL Server with AdventureWorks Sample Database
docker run -d --rm -it -p 1453:1433 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=nc_Test_Pipeline!" chriseaton/adventureworks:latest
```
