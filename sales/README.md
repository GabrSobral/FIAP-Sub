# Serviço de Venda de Veículos — Listagens e Compras

FIAP SOAT / Pós-Tech — Trabalho substitutivo do Tech Challenge, Fase 4.

Este é o **repositório do serviço de venda de veículos**. Ele existe isolado, com **banco de dados
próprio**, porque o enunciado determina que *"para suportar o aumento repentino de chamadas, os
endpoints de listagem e compras de veículos devem estar isolados em um serviço único, rodando com
um banco de dados isolado"*.

O catálogo de veículos e o webhook de pagamento ficam no [serviço core](../core), que usa um banco
de dados **diferente** deste. A comunicação entre os dois é feita só por **HTTP**.

## O que este serviço faz

- **Listagem de veículos à venda**, ordenada por preço, do mais barato para o mais caro
- **Listagem de veículos vendidos**, ordenada por preço, do mais barato para o mais caro
- **Efetua a venda** de um veículo: CPF de quem comprou e data da venda
- Recebe do serviço core a réplica dos veículos e o resultado de cada pagamento

As duas listagens são atendidas **inteiramente pelo banco deste serviço**. Nenhuma requisição de
listagem viaja até o core — é isso que faz um pico de tráfego aqui não virar carga lá.

## Como foi implementado

Clean Architecture, uma biblioteca C# por camada, nomeadas `FiapSub.Sales.<camada>.<subcamada>`:

```
src/
  FiapSub.Sales.Domain                    agregados, value objects, eventos, interfaces de repositório
  FiapSub.Sales.Application               casos de uso, Result/Error, validações, contratos de gateway
  FiapSub.Sales.Infrastructure.Database   EF Core: DbContext, mapeamentos, repositórios, migrations
  FiapSub.Sales.Infrastructure.Http       HttpClient tipado para o serviço core
  FiapSub.Sales.Api                       controllers, middlewares, health checks, Swagger
tests/
  FiapSub.Sales.Domain.Tests              invariantes e transições dos agregados, validação de CPF
  FiapSub.Sales.Application.Tests         casos de uso e a saga da compra, com dublês
  FiapSub.Sales.Infrastructure.Tests      classificação das respostas do core, com handler dublê
  FiapSub.Sales.Api.IntegrationTests      pipeline real + PostgreSQL via Testcontainers
```

A regra de dependência é imposta pelas referências de projeto: o `Domain` não referencia nenhum
projeto nem pacote. `Api -> Application -> Domain`, `Infrastructure.* -> Application -> Domain`, e
o `Api` conhece a infraestrutura apenas no ponto de composição.

### Domínio (DDD)

| Agregado | Comportamento | Invariantes |
|---|---|---|
| `Sale` | `Open`, `MarkPaid`, `Cancel` | CPF válido; valor > 0; data da venda não pode ser futura; só uma venda `PendingPayment` pode ser paga ou cancelada |
| `VehicleSnapshot` | `Replicate`, `TryApplyUpdate`, `ProjectLocalStatus` | só aplica atualização com versão **estritamente maior**; preço > 0; status conhecido |

Value objects: `Cpf` (remove a máscara, rejeita dígitos repetidos e **valida os dois dígitos
verificadores**), `Money`, `PaymentCode`. Um CPF inválido nunca chega ao banco.

Eventos de domínio `SaleOpened`, `SalePaid`, `SaleCancelled` são despachados pelo `UnitOfWork`
**depois** do commit e viram a trilha de auditoria da venda — assim o log nunca afirma algo que foi
desfeito.

### A compra é uma saga com compensação

Uma compra atravessa os dois serviços, então não há transação única. A ordem é deliberada:

1. a venda é gravada **aqui primeiro** — nunca se reserva um veículo sem haver registro do motivo;
2. o veículo sai da listagem de imediato (`ProjectLocalStatus`), sem fingir ser uma versão nova do
   catálogo: o próximo push autoritativo do core sempre prevalece;
3. pede-se ao core `POST /api/internal/vehicles/{id}/reserve`;
4. se o core **recusa** (`404`/`409`), a venda é cancelada e o resultado volta como `404`/`409`;
5. se o core **não responde**, a reserva pode ter sido efetivada antes da queda — então a venda é
   cancelada **e** chama-se `release` por precaução.

O `VehicleCatalogueGateway` existe justamente para separar *"o core disse não"* de *"o core não
disse nada"*, porque a compensação é diferente em cada caso.

### Idempotência

As chamadas entre serviços usam retry, timeout e circuit breaker
(`Microsoft.Extensions.Http.Resilience`), ou seja, são **at-least-once**. Todo endpoint receptor
aguenta repetição:

| Operação | O que a torna idempotente |
|---|---|
| `PUT /api/internal/vehicles/{id}` | só aplica versão estritamente maior; replays e chegadas fora de ordem são ignorados |
| `POST /api/internal/payments/result` | `MarkPaid`/`Cancel` são no-op quando a venda já está no estado final |
| Compra duplicada | índice único parcial `ux_sales_pending_vehicle` (`WHERE status = 'PendingPayment'`) |

### Persistência — EF Core (code-first)

Único mecanismo de acesso a dados: sem Dapper, sem ADO.NET cru, sem SQL escrito à mão.

- `SalesDbContext` com `DbSet` apenas para raízes de agregado;
- um `IEntityTypeConfiguration<T>` por agregado, carregado por `ApplyConfigurationsFromAssembly` —
  o `Domain` não tem data annotations nem referência ao EF;
- `Money` mapeado por `ComplexProperty`; `Cpf` e `PaymentCode` por `HasConversion`; enums como
  texto;
- concorrência otimista pelo `Version` do agregado (`IsConcurrencyToken`), devolvida como `409`;
- índices: único em `sales.payment_code`, único **parcial** em `sales.vehicle_id` filtrado por
  `status = 'PendingPayment'`, e `(status, price)` em `vehicle_snapshots` — esse último é o índice
  que sustenta as duas listagens ordenadas por preço;
- **migrations versionadas** em `src/FiapSub.Sales.Infrastructure.Database/Migrations`, aplicadas
  por `Database.MigrateAsync()` no start quando `Database:RunMigrationsOnStartup` está ligado.
  `EnsureCreated` não é usado.

O `VehicleSnapshot.Price` é um `decimal` simples, e não um `Money`: é um read model cuja razão de
existir é ser ordenado por preço em SQL, e uma coluna escalar é o que o índice composto consegue
cobrir.

```bash
dotnet ef migrations add <Nome> \
  --project src/FiapSub.Sales.Infrastructure.Database \
  --startup-project src/FiapSub.Sales.Api
```

## Como usar localmente

O caminho mais curto é subir a stack inteira pela raiz do repositório:

```bash
docker compose up --build        # core-db, core-api, sales-db, sales-api
```

Swagger em <http://localhost:8081/swagger>.

Para rodar apenas este serviço, sem container:

```bash
docker compose up -d sales-db core-api   # o banco deste serviço e o core, de quem ele depende
dotnet run --project src/FiapSub.Sales.Api
```

O perfil `Development` já aponta para `localhost:5433` e usa `dev-internal-token` como segredo
compartilhado. Em produção, `Database:ConnectionString`, `InternalApi:Token` e
`CatalogueService:InternalToken` vêm de variáveis de ambiente ou de um secret do Kubernetes — o
`appsettings.json` versionado deixa os tokens **vazios de propósito**, e as rotas internas
respondem `503` até que sejam configurados.

### Exemplo de uso

```bash
# veículos à venda, do mais barato para o mais caro
curl 'http://localhost:8081/api/vehicles/for-sale?page=1&pageSize=20'

# comprar: CPF de quem comprou (saleDate é opcional)
curl -X POST http://localhost:8081/api/sales \
  -H 'Content-Type: application/json' \
  -d '{"vehicleId":"<id>","cpf":"529.982.247-25"}'
# -> 201 com o paymentCode a ser informado ao provedor de pagamento

# veículos vendidos, do mais barato para o mais caro
curl 'http://localhost:8081/api/vehicles/sold'
```

**Sobre a data da venda:** `saleDate` é opcional. Se informado, é ele que fica registrado; se
omitido, usa-se o instante em que o pagamento foi aprovado. Uma venda só ganha data quando o
pagamento é confirmado — antes disso ela está `PendingPayment`.

**Primeira subida:** as listagens começam vazias, porque a projeção é preenchida pelos pushes do
core. Para preencher de uma vez (ou reconciliar algo que um push tenha perdido):

```bash
curl -X POST 'http://localhost:8081/api/internal/catalogue/sync' \
  -H 'X-Internal-Token: local-dev-internal-token'
```

## Como testar

```bash
dotnet test --solution FiapSub.Sales.slnx
```

Com cobertura, do mesmo jeito que o CI:

```bash
dotnet test --solution FiapSub.Sales.slnx \
  --coverage --coverage-settings coverage.runsettings \
  --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/**/TestResults/coverage.cobertura.xml" \
  -targetdir:coveragereport -reporttypes:"Html;TextSummary"
cat coveragereport/Summary.txt
```

**197 testes, 100% de cobertura de linha** (mínimo exigido: 80%; o pipeline falha abaixo disso).

Os testes de integração sobem um PostgreSQL real com Testcontainers e aplicam as migrations, então
o Docker precisa estar em execução. Para pular essa parte:

```bash
dotnet test --solution FiapSub.Sales.slnx --filter-not-trait "Category=Integration"
```

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/vehicles/for-sale?page=&pageSize=` | Veículos à venda, preço crescente |
| `GET` | `/api/vehicles/sold?page=&pageSize=` | Veículos vendidos, preço crescente |
| `POST` | `/api/sales` | Efetua a venda → `201` com o `paymentCode` |
| `GET` | `/api/sales/{id}` | Consulta a venda (status, `paymentCode`, data da venda) |
| `PUT` | `/api/internal/vehicles/{id}` | 🔒 Recebe a réplica do veículo |
| `POST` | `/api/internal/payments/result` | 🔒 Recebe o resultado do pagamento |
| `POST` | `/api/internal/catalogue/sync` | 🔒 Reconstrói a projeção a partir do catálogo |
| `GET` | `/health/live` · `/health/ready` | Probes (`ready` também verifica o banco) |

🔒 = exige o header `X-Internal-Token`. Essas rotas ficam fora do Ingress.

O CPF é devolvido **mascarado** (`529.982.247-25`), nunca cru: é dado pessoal e nada a jusante
precisa dos dígitos sem máscara.

## Configuração

| Chave | Variável de ambiente | Observação |
|---|---|---|
| `Database:ConnectionString` | `Database__ConnectionString` | PostgreSQL deste serviço |
| `Database:RunMigrationsOnStartup` | `Database__RunMigrationsOnStartup` | aplica as migrations no start |
| `CatalogueService:BaseUrl` | `CatalogueService__BaseUrl` | URL do serviço core |
| `CatalogueService:InternalToken` | `CatalogueService__InternalToken` | token enviado ao core |
| `InternalApi:Token` | `InternalApi__Token` | token exigido nas rotas internas deste serviço |

`InternalApi:Token` e o `SalesService:InternalToken` do serviço core devem ter o **mesmo valor** —
é o segredo compartilhado entre os dois.

## Este diretório é um repositório completo

Tudo o que o serviço precisa está aqui: `global.json` (que fixa o SDK e liga o runner de testes),
`coverage.runsettings`, `Directory.Build.props`, `Directory.Packages.props`, `Dockerfile`, `k8s/`,
`.gitignore` e `.dockerignore`. Depois do `git subtree split` descrito no README da raiz, este
diretório vira a raiz do repositório e `dotnet build`, `dotnet test` e `docker build .` funcionam
sem nenhum ajuste — isso é verificado, não presumido.

A única edição necessária é no workflow: mova `.github/workflows/sales.yml` para
`.github/workflows/ci-cd.yml` e troque `SERVICE_DIR: sales` por `SERVICE_DIR: .`. Todos os demais
caminhos do pipeline derivam dessa variável.

## CI/CD

`.github/workflows/sales.yml` (na raiz do monorepo):

- **Pull request** → restore, build com `-warnaserror`, testes com cobertura e **falha abaixo de
  80%**; o relatório HTML fica como artefato para download.
- **Merge na `main`** → o mesmo gate, build da imagem, push no GHCR,
  `kubectl apply -f k8s/`, `kubectl set image` apontando para a imagem do commit e
  `rollout status`. O passo de `kubectl` é condicionado ao secret `KUBE_CONFIG` e é **pulado**
  (não falha) quando não há cluster configurado.

`k8s/` traz o conjunto completo de publicação: `Deployment` (2 réplicas, probes, não-root, root FS
read-only), `Service` (`ClusterIP`), `HorizontalPodAutoscaler` de 2 a 10 réplicas por CPU — a
resposta direta ao "aumento repentino de chamadas" —, `Ingress`, `ConfigMap`, modelo de `Secret` e
o `StatefulSet` do banco próprio deste serviço.

```bash
kubectl apply -f k8s/
```
