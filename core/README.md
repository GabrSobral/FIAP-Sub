# Serviço Core — Catálogo de Veículos

FIAP SOAT / Pós-Tech — Trabalho substitutivo do Tech Challenge, Fase 4.

Este é o **repositório das demais funcionalidades do software**. Ele é dono do catálogo de
veículos e recebe o webhook do provedor de pagamento. As **listagens públicas e a compra** vivem no
[serviço de venda de veículos](../sales), que roda com um **banco de dados diferente** deste.

## O que este serviço faz

- Cadastra um veículo para venda (marca, modelo, ano, cor, preço)
- Edita os dados do veículo
- Expõe o **webhook** pelo qual o provedor de pagamento informa, a partir do código do pagamento,
  se ele foi **efetuado** ou **cancelado**
- Reserva e libera veículos quando o serviço de vendas pede (rotas internas)
- Publica cada mudança de veículo para o serviço de vendas, por HTTP

Este serviço é a **única fonte de verdade** do estado de um veículo
(`Available` → `Reserved` → `Sold`). O serviço de vendas mantém apenas uma réplica.

## Como foi implementado

Clean Architecture, uma biblioteca C# por camada, nomeadas `FiapSub.Core.<camada>.<subcamada>`:

```
src/
  FiapSub.Core.Domain                    agregados, value objects, eventos, interfaces de repositório
  FiapSub.Core.Application               casos de uso, Result/Error, validações, contratos de gateway
  FiapSub.Core.Infrastructure.Database   EF Core: DbContext, mapeamentos, repositórios, migrations
  FiapSub.Core.Infrastructure.Http       HttpClient tipado para o serviço de vendas
  FiapSub.Core.Api                       controllers, middlewares, health checks, Swagger
tests/
  FiapSub.Core.Domain.Tests              invariantes e transições dos agregados
  FiapSub.Core.Application.Tests         casos de uso, com repositórios e gateways dublês
  FiapSub.Core.Infrastructure.Tests      contrato HTTP de saída, com HttpMessageHandler dublê
  FiapSub.Core.Api.IntegrationTests      pipeline real + PostgreSQL via Testcontainers
```

A regra de dependência é imposta pelas referências de projeto: o `Domain` não referencia nenhum
projeto nem pacote. `Api -> Application -> Domain`, `Infrastructure.* -> Application -> Domain`, e
o `Api` conhece a infraestrutura apenas no ponto de composição.

### Domínio (DDD)

| Agregado | Comportamento | Invariantes |
|---|---|---|
| `Vehicle` | `Register`, `UpdateDetails`, `Reserve`, `ConfirmSale`, `ReleaseReservation` | preço > 0; ano entre 1900 e o próximo; marca/modelo/cor não vazios; só um veículo `Available` é editável ou reservável; confirmar ou liberar exige o mesmo `paymentCode` da reserva |
| `PaymentNotification` | `Record`, `Retry`, `MarkApplied`, `MarkUnapplied` | um registro por código de pagamento (índice único) — é isso que torna o webhook idempotente; depois de aplicada é final, enquanto não encontra veículo aceita o reenvio do provedor |

Value objects: `Money` (valor + moeda, no máximo 2 casas), `ModelYear`, `PaymentCode`.
Eventos de domínio: `VehicleRegistered`, `VehicleDetailsUpdated`, `VehicleReserved`, `VehicleSold`,
`VehicleReservationReleased` — coletados no agregado e despachados pelo `UnitOfWork` **depois** do
commit, de modo que nenhum handler reaja a uma mudança desfeita.

### Persistência — EF Core (code-first)

Único mecanismo de acesso a dados: sem Dapper, sem ADO.NET cru, sem SQL escrito à mão.

- `CoreDbContext` com `DbSet` apenas para raízes de agregado;
- um `IEntityTypeConfiguration<T>` por agregado, carregado por `ApplyConfigurationsFromAssembly` —
  o `Domain` não tem data annotations nem referência ao EF;
- `Money` mapeado por `ComplexProperty` (colunas `price` e `price_currency`); `ModelYear` e
  `PaymentCode` por `HasConversion`; enums como texto;
- concorrência otimista pelo `Version` do agregado (`IsConcurrencyToken`), devolvida como `409`;
- índices: único em `payment_notifications.payment_code`, único em
  `vehicles.reservation_payment_code`, e um índice por `status`;
- **migrations versionadas** em `src/FiapSub.Core.Infrastructure.Database/Migrations`, aplicadas
  por `Database.MigrateAsync()` no start quando `Database:RunMigrationsOnStartup` está ligado.
  `EnsureCreated` não é usado.

```bash
# novas migrations
dotnet ef migrations add <Nome> \
  --project src/FiapSub.Core.Infrastructure.Database \
  --startup-project src/FiapSub.Core.Api

# conferir que o modelo e as migrations estão em sincronia
dotnet ef migrations has-pending-model-changes \
  --project src/FiapSub.Core.Infrastructure.Database \
  --startup-project src/FiapSub.Core.Api
```

## Como usar localmente

O caminho mais curto é subir a stack inteira pela raiz do repositório:

```bash
docker compose up --build        # core-db, core-api, sales-db, sales-api
```

Swagger em <http://localhost:8080/swagger>.

Para rodar apenas este serviço, sem container:

```bash
docker compose up -d core-db     # só o banco deste serviço
dotnet run --project src/FiapSub.Core.Api
```

O perfil `Development` já aponta para `localhost:5432` e usa `local-dev-internal-token` (o mesmo
do `docker-compose.yml`) como segredo compartilhado. Em produção, `Database:ConnectionString`,
`InternalApi:Token` e `SalesService:InternalToken` vêm de variáveis de ambiente ou de um secret do Kubernetes — o
`appsettings.json` versionado deixa os tokens **vazios de propósito**, e as rotas internas
respondem `503` até que sejam configurados.

### Exemplo de uso

```bash
# cadastrar
curl -X POST http://localhost:8080/api/vehicles \
  -H 'Content-Type: application/json' \
  -d '{"brand":"Fiat","model":"Argo Drive 1.3","year":2023,"color":"Vermelho","price":78900.00}'

# o provedor de pagamento avisa o resultado
curl -X POST http://localhost:8080/api/webhooks/payments \
  -H 'Content-Type: application/json' \
  -d '{"paymentCode":"PAY-...","status":"approved"}'
```

O webhook aceita `approved`/`paid`/`aprovado` e `cancelled`/`canceled`/`rejected`/`cancelado`, sem
diferenciar maiúsculas. Qualquer outro valor é `400` — o serviço não adivinha.

## Como testar

```bash
dotnet test --solution FiapSub.Core.slnx
```

Com cobertura, do mesmo jeito que o CI:

```bash
rm -rf TestResults   # cada execução gera relatórios novos; comece limpo
dotnet test --solution FiapSub.Core.slnx \
  --coverage --coverage-settings coverage.runsettings \
  --results-directory TestResults --coverage-output-format cobertura

dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/*.cobertura.xml" \
  -targetdir:coveragereport -reporttypes:"Html;TextSummary"
cat coveragereport/Summary.txt
```

**177 testes, 99,8% de cobertura de linha** (mínimo exigido: 80%; o pipeline falha abaixo disso).

Os testes de integração sobem um PostgreSQL real com Testcontainers e aplicam as migrations, então
o Docker precisa estar em execução. Para pular essa parte:

```bash
dotnet test --solution FiapSub.Core.slnx --filter-not-trait "Category=Integration"
```

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/vehicles` | Cadastra um veículo → `201` |
| `PUT` | `/api/vehicles/{id}` | Edita o veículo (`409` se não estiver `Available`) |
| `GET` | `/api/vehicles/{id}` | Consulta |
| `GET` | `/api/vehicles?status=&page=&pageSize=` | Listagem operacional |
| `POST` | `/api/webhooks/payments` | Webhook de pagamento, idempotente |
| `POST` | `/api/internal/vehicles/{id}/reserve` | 🔒 Reserva para uma compra |
| `POST` | `/api/internal/vehicles/{id}/release` | 🔒 Libera a reserva |
| `GET` | `/api/internal/vehicles` | 🔒 Exporta o catálogo |
| `GET` | `/health/live` · `/health/ready` | Probes (`ready` também verifica o banco) |

🔒 = exige o header `X-Internal-Token`. Essas rotas ficam fora do Ingress.

## Configuração

| Chave | Variável de ambiente | Observação |
|---|---|---|
| `Database:ConnectionString` | `Database__ConnectionString` | PostgreSQL deste serviço |
| `Database:RunMigrationsOnStartup` | `Database__RunMigrationsOnStartup` | aplica as migrations no start |
| `SalesService:BaseUrl` | `SalesService__BaseUrl` | URL do serviço de vendas |
| `SalesService:InternalToken` | `SalesService__InternalToken` | token enviado ao serviço de vendas |
| `InternalApi:Token` | `InternalApi__Token` | token exigido nas rotas internas deste serviço |

`InternalApi:Token` e o `CatalogueService:InternalToken` do serviço de vendas devem ter o **mesmo
valor** — é o segredo compartilhado entre os dois.

## Este diretório é um repositório completo

Tudo o que o serviço precisa está aqui: `global.json` (que fixa o SDK e liga o runner de testes),
`coverage.runsettings`, `Directory.Build.props`, `Directory.Packages.props`, `Dockerfile`, `k8s/`,
`.gitignore` e `.dockerignore`. Depois do `git subtree split` descrito no README da raiz, este
diretório vira a raiz do repositório e `dotnet build`, `dotnet test` e `docker build .` funcionam
sem nenhum ajuste — isso é verificado, não presumido.

A única edição necessária é no workflow, que mora na raiz do monorepo e não entra no split:
copie `.github/workflows/core.yml` para `.github/workflows/ci-cd.yml` no repositório novo e troque `SERVICE_DIR: core` por `SERVICE_DIR: .`. Todos os demais
caminhos do pipeline derivam dessa variável.

## CI/CD

`.github/workflows/core.yml` (na raiz do monorepo):

- **Pull request** → restore, build com `-warnaserror`, testes com cobertura e **falha abaixo de
  80%**; o relatório HTML fica como artefato para download.
- **Merge na `main`** → o mesmo gate, build da imagem, push no GHCR e deploy por
  `scripts/deploy.sh`: cria os secrets, aplica `k8s/` com a imagem do commit, espera o
  `rollout status` e faz um smoke test pelo `Service`. Com o secret `KUBE_CONFIG` o deploy vai para
  esse cluster; sem ele, para um cluster **kind** efêmero no próprio runner — o deploy nunca é pulado.

`k8s/` traz o conjunto completo de publicação: `Deployment` (2 réplicas, probes, não-root, root FS
read-only), `Service` (`ClusterIP`), `HorizontalPodAutoscaler`, `Ingress`, `ConfigMap`, modelo de
`Secret` e o `StatefulSet` do banco próprio deste serviço.

O `secret.example.yaml` é só um modelo; os secrets reais são criados pelo script, a partir de
`DB_PASSWORD` e `INTERNAL_TOKEN` (ou gerados no primeiro deploy e mantidos depois). Para publicar
no cluster para o qual o `kubectl` aponta — um `kind` local, por exemplo:

```bash
docker build -t fiapsub-core:local .
kind load docker-image fiapsub-core:local --name fiap-sub
IMAGE=fiapsub-core:local INTERNAL_TOKEN=demo ./scripts/deploy.sh
```

