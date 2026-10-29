# hortalicas-be

API .NET 8 do Hortaliças: caderno de campo das estufas (plantio, irrigação e colheita) e painel de acompanhamento.

## Rodar

```bash
dotnet run --project src/Hortalicas.Api --launch-profile http
```

API em `http://localhost:5259` e Swagger em `http://localhost:5259/swagger`.
Os dados ficam no arquivo SQLite `src/Hortalicas.Api/hortalicas.db`, criado e atualizado sozinho quando a API sobe.

## Como funciona

- **Estufa:** nome, comprimento e largura (área em m²), quantidade de canteiros (criados numerados de 1 a N) e em quantos dias sem irrigação ela entra em alerta.
- **Hortaliça:** nome e ciclo em dias, do plantio até a colheita.
- **Plantio:** hortaliça num canteiro livre. A colheita prevista é a data do plantio + o ciclo.
- **Colheita:** peso aproveitado e descartado (com motivo) em gramas. A colheita final encerra o plantio e libera o canteiro; a parcial mantém o plantio. Cada colheita recebe um lote (data + código).
- **Encerrar plantio:** para perda sem colheita (praga, pendoamento, clima), com motivo.
- **Irrigação:** estufa, data, hora (opcional) e minutos ligada.

Situação do canteiro no painel: vazio, em crescimento, pronto para colher (até 3 dias depois da data prevista), atrasado ou em colheita (já teve colheita parcial).
A estufa com plantio em andamento entra em alerta quando passa do limite de dias sem irrigação.

| Rota | O que mostra |
|---|---|
| `GET /api/painel` | um card por estufa |
| `GET /api/estufas/{id}/mapa` | visão de cima da estufa, canteiro a canteiro |
| `GET /api/painel/hoje` | o que colher hoje e as estufas sem irrigação |
| `GET /api/painel/resumo?ano=&mes=` | colhido por hortaliça e por estufa (g/m²), descarte e irrigação no mês |

## Testar

```bash
dotnet test
```

Os testes usam SQLite em memória e o relógio parado em 28/10/2026.

## Banco

Nova mudança de modelo:

```bash
dotnet tool restore
dotnet ef migrations add <Nome> -p src/Hortalicas.Api -o Data/Migrations
```

## Contrato OpenAPI

```bash
dotnet tool restore
dotnet build src/Hortalicas.Api
dotnet swagger tofile --output docs/openapi.json src/Hortalicas.Api/bin/Debug/net8.0/Hortalicas.Api.dll v1
```
