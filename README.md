# hortalicas-be

API .NET 8 do Hortaliças.

## Rodar

```bash
dotnet run --project src/Hortalicas.Api --launch-profile http
```

API em `http://localhost:5259` e Swagger em `http://localhost:5259/swagger`.
Os dados ficam em memória (EF Core InMemory) e somem ao reiniciar.

## Testar

```bash
dotnet test
```

## Contrato OpenAPI

```bash
dotnet tool restore
dotnet build src/Hortalicas.Api
dotnet swagger tofile --output docs/openapi.json src/Hortalicas.Api/bin/Debug/net8.0/Hortalicas.Api.dll v1
```
