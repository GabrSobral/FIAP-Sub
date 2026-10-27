# Roteiro do vídeo de demonstração

Trabalho substitutivo — Tech Challenge SOAT, Fase 4.

O enunciado pede um vídeo *"demonstrando a solução funcionando, tanto na infraestrutura quanto no
uso, fazendo um teste ponta-a-ponta, com cadastro de veículo para venda, compra e efetivação da
compra, mostrando a implementação e o uso dos serviços funcionando de forma segregada, mas se
comunicando, com os testes rodando e mostrando evidências da cobertura dos testes."*

Cada exigência está mapeada em um bloco abaixo. Todos os comandos deste roteiro foram executados
e conferidos.

| O que o enunciado exige | Bloco | Duração |
|---|---|---|
| Infraestrutura funcionando | 1 | ~2 min |
| Teste ponta a ponta: cadastro → compra → efetivação | 3 | ~6 min |
| Serviços segregados, mas se comunicando | 2 e 4 | ~3 min |
| Testes rodando | 5 | ~2 min |
| Evidências da cobertura | 6 | ~2 min |
| CI/CD com PR e deploy no merge | 7 | ~2 min |

Total: **15 a 18 minutos**.

---

## Bloco 0 — Preparação (antes de apertar o REC)

```bash
cd ~/www/fiap-sub

# Zera os bancos. Sem isso a vitrine aparece poluída com os dados de testes anteriores,
# e a ordenação por preço fica difícil de ler na tela.
docker compose down -v

docker compose up --build -d

# Espere os cinco containers ficarem healthy antes de gravar.
watch -n 2 'docker compose ps --format "table {{.Service}}\t{{.Status}}"'
```

Deixe abertas, em abas do navegador:

1. <http://localhost:3000> — a interface
2. <http://localhost:8080/swagger> — API do core
3. <http://localhost:8081/swagger> — API do sales

E um terminal com fonte grande (14pt+). Grave em 1080p; a tabela do Catálogo tem colunas estreitas.

**Dados que vamos usar** (já escolhidos para a demonstração ficar clara):

| Veículo | Marca | Modelo | Ano | Cor | Preço |
|---|---|---|---|---|---|
| A | Toyota | Corolla XEi | 2024 | Prata | `164900.00` |
| B | Volkswagen | Gol 1.0 | 2019 | Branco | `41500.00` |
| C | Fiat | Argo Drive 1.3 | 2023 | Vermelho | `78900.00` |

Cadastre **nesta ordem** (A, B, C) — de propósito fora da ordem de preço. Assim a listagem
ordenada prova que o ordenamento é da consulta, e não da ordem de inserção.

CPFs válidos para usar: `529.982.247-25` e `111.444.777-35`.

---

## Bloco 1 — Infraestrutura (~2 min)

> Fala: *"Dois serviços independentes, cada um com o seu próprio banco PostgreSQL."*

```bash
docker compose ps
```

Mostre os cinco containers: `core-api`, `core-db`, `sales-api`, `sales-db` e `web`.

**Dois bancos, de verdade separados** — este é o print mais importante do bloco:

```bash
docker compose exec core-db  psql -U fiapsub -d fiapsub_core  -c '\dt'
docker compose exec sales-db psql -U fiapsub -d fiapsub_sales -c '\dt'
```

Aponte na tela: o `core-db` só tem `vehicles` e `payment_notifications`; o `sales-db` só tem
`sales` e `vehicle_snapshots`. Cada um com o seu próprio `__ef_migrations_history` — ou seja, as
migrations do EF Core foram aplicadas separadamente em cada banco.

```bash
# Nenhum dos dois tem rota para o banco do outro.
docker compose exec core-api  printenv Database__ConnectionString
docker compose exec sales-api printenv Database__ConnectionString
```

**Manifests de publicação** (o enunciado pede os *deployments* e *services*):

```bash
ls core/k8s sales/k8s
grep -h '^kind:' core/k8s/*.yaml | sort -u
```

**Os mesmos manifests rodando num Kubernetes de verdade** — é exatamente o que o pipeline faz a
cada merge. Prepare antes de gravar (leva ~2 min), em outro terminal, com o `docker compose` parado
para liberar as portas 8080/8081:

```bash
kind create cluster --name fiap-sub
docker build -t fiapsub-core:local core && docker build -t fiapsub-sales:local sales
kind load docker-image fiapsub-core:local fiapsub-sales:local --name fiap-sub
IMAGE=fiapsub-core:local  INTERNAL_TOKEN=demo core/scripts/deploy.sh
IMAGE=fiapsub-sales:local INTERNAL_TOKEN=demo sales/scripts/deploy.sh
```

Na gravação, mostre o cluster e rode o ponta a ponta através dos `Service`s:

```bash
kubectl -n fiap-sub get statefulset,deployment,service,hpa,ingress,pods
kubectl -n fiap-sub port-forward svc/core-api  8080:8080 &
kubectl -n fiap-sub port-forward svc/sales-api 8081:8080 &
INTERNAL_TOKEN=demo ./scripts/e2e.sh
```

> Fala: *"Cada serviço com o seu Deployment, Service, autoscaler e o seu próprio banco em um
> StatefulSet. O mesmo script de deploy é o que o pipeline roda depois de cada merge."*

Depois: `pkill -f 'kubectl -n fiap-sub port-forward'` e `kind delete cluster --name fiap-sub`.

---

## Bloco 2 — Os dois serviços, lado a lado (~1 min)

> Fala: *"Cada serviço expõe só aquilo de que é responsável."*

Abra os dois Swaggers e compare:

- **core (8080)** — `POST/PUT/GET /api/vehicles` e `POST /api/webhooks/payments`
- **sales (8081)** — `GET /api/vehicles/for-sale`, `GET /api/vehicles/sold`, `POST /api/sales`

> Fala: *"As listagens e a compra estão isoladas no serviço de vendas, com banco isolado, como o
> enunciado exige. O catálogo e o webhook de pagamento estão no core."*

---

## Bloco 3 — Ponta a ponta na interface (~6 min)

Abra <http://localhost:3000>.

No rodapé do rail escuro da esquerda, aponte as duas sondas — **`core :8080`** e
**`vendas :8081`**, cada uma com o ponto verde. Elas consultam `/health/ready` de cada serviço,
que inclui o banco de cada um, a cada dez segundos.

> As três seções ficam nesse rail. Os atalhos **1**, **2** e **3** trocam de seção sem o mouse —
> útil para a gravação fluir.

### 3.1 — Cadastrar os veículos (seção **Catálogo**)

1. Abra a seção **Catálogo** (atalho `2`). Estado inicial: *"Catálogo vazio"*.
2. Clique em **Cadastrar veículo**.
3. Preencha o veículo **A** (Toyota Corolla XEi, 2024, Prata, `164900.00`) e clique em
   **Cadastrar**.
4. Repita para **B** (Volkswagen Gol 1.0) e **C** (Fiat Argo Drive 1.3).

A cada cadastro aparece um aviso: *"… cadastrado e publicado na vitrine"*. A tabela mostra marca,
modelo, ano, cor, preço, status **À venda** e a **versão** do agregado.

> Fala: *"Cadastrei fora da ordem de preço: primeiro o mais caro, depois o mais barato."*

### 3.2 — Listagem ordenada por preço (seção **Vitrine**)

5. Abra a seção **Vitrine** (atalho `1`), com **À venda** selecionado.

Os três aparecem numerados **01, 02, 03** na ordem **41.500 → 78.900 → 164.900**, com os
algarismos alinhados em coluna — a ordem crescente se lê de relance.

> Fala: *"Do mais barato para o mais caro, como o enunciado pede. E note que a ordem de cadastro
> foi outra. Esta listagem vem do banco do serviço de vendas — nenhuma chamada chegou ao core."*

### 3.3 — Editar um veículo

6. Volte ao **Catálogo**, clique em **Editar** no Fiat.
7. Mude a cor para `Azul` e o preço para `69900.00`. Clique em **Salvar**.
8. Volte à **Vitrine** → **À venda**: o Fiat aparece com o novo preço, **e a ordem mudou**
   (69.900 passa a ser o segundo mais barato... ou o primeiro, dependendo do valor).

> Fala: *"A edição no core foi publicada para o serviço de vendas por HTTP, e a listagem já
> reflete o novo preço."*

### 3.4 — Efetuar a compra

9. Na **Vitrine** → **À venda**, clique em **Comprar** no **Fiat Argo**.
10. No diálogo, digite o CPF `529.982.247-25` — a máscara se aplica enquanto você digita.
    Deixe **Data da venda** em branco.
11. Clique em **Confirmar compra**.

Aparece o aviso com o **código de pagamento** (`PAY-…`). A interface muda para a aba
**Pagamentos**.

> **Mostre o CPF inválido também** (vale 15 segundos): digite `529.982.247-26` e aponte o erro
> *"Dígito verificador inválido"*. Mesma validação do value object `Cpf` no domínio.

12. Volte à **Vitrine** → **À venda**: o Fiat **saiu da listagem**.
13. Vá ao **Catálogo**: o Fiat está **Reservado**, com o código do pagamento na coluna, e o botão
    **Editar** está **desabilitado** — passe o mouse para mostrar o motivo.

> Fala: *"A compra abriu uma venda aguardando pagamento, e o serviço de vendas pediu ao core que
> reservasse o veículo. O core é a única fonte de verdade do status."*

### 3.5 — Efetivação da compra (webhook do pagamento)

14. Seção **Pagamentos** (atalho `3`). A venda aparece com status **Aguardando pagamento**, com CPF, valor e
    código do pagamento.
15. Clique em **Aprovar**.

> Fala: *"Este botão faz o papel da entidade que processa o pagamento: ele chama o webhook real do
> core, passando o código do pagamento."*

Aviso: *"Pagamento aprovado. Veículo em Sold."* A linha vira **Pago**, com a **data da venda**
preenchida.

16. Vá à **Vitrine** → **Vendidos**: o Fiat aparece, também ordenado por preço.
17. Vá ao **Catálogo**: o Fiat está **Vendido**.

### 3.6 — Idempotência do webhook

18. Na seção **Pagamentos**, no bloco **Notificar um código à mão**, cole o **mesmo** código de
    pagamento e clique em **Aprovar**.

Aviso: *"Esse código já havia sido notificado — nada mudou (idempotente)."*

> Fala: *"O provedor de pagamento pode reenviar a notificação. Um índice único no registro de
> notificações garante que o reenvio não muda nada."*

### 3.7 — Data da venda informada

19. **Vitrine** → **Comprar** no **Gol**, CPF `111.444.777-35`, e agora **preencha a Data da
    venda** com uma data passada.
20. **Pagamentos** → **Aprovar**.
21. A coluna **Data da venda** mostra **a data que você informou**, não a do momento da aprovação.

> Fala: *"A data da venda pode vir na requisição; em branco, vale o instante em que o pagamento
> foi aprovado."*

### 3.8 — Pagamento cancelado

22. **Vitrine** → **Comprar** no **Corolla**, CPF `529.982.247-25`.
23. **Pagamentos** → **Cancelar**.
24. A venda vira **Cancelado**. Vá à **Vitrine** → **À venda**: o **Corolla voltou** para a
    listagem.
25. No **Catálogo**, o Corolla está **À venda** de novo e já pode ser editado.

### 3.9 — Consultar uma venda

26. Seção **Pagamentos** → bloco **Consultar uma venda**. Cole o id de uma venda (está na primeira
    coluna da tabela) e clique em **Consultar**.
27. Mostra status, comprador (CPF mascarado), valor, código e data da venda.

---

## Bloco 4 — Segregados, mas se comunicando (~2 min)

> Fala: *"Agora a prova de que os dois serviços conversam, e só por HTTP."*

```bash
# core publicando cada mudança de veículo para o serviço de vendas
docker compose logs core-api | grep 'Published vehicle' | tail -5

# sales pedindo ao core que reserve o veículo
docker compose logs sales-api | grep 'holding vehicle' | tail -3

# core informando ao sales o resultado do pagamento
docker compose logs core-api | grep 'Reported payment' | tail -3

# a trilha de auditoria da venda, no serviço de vendas
docker compose logs sales-api | grep -E 'opened for vehicle|was paid|was cancelled' | tail -5
```

**As rotas entre serviços não são públicas:**

```bash
# sem o token compartilhado -> 401
curl -i -X POST http://localhost:8080/api/internal/vehicles/00000000-0000-0000-0000-000000000000/reserve \
  -H 'Content-Type: application/json' -d '{"paymentCode":"PAY-x"}' | head -1

# e o proxy da interface nem deixa o navegador chegar lá -> 404
curl -o /dev/null -s -w '%{http_code}\n' http://localhost:3000/core-api/api/internal/vehicles
```

### Opcional, mas forte: o script ponta a ponta

```bash
./scripts/e2e.sh
```

Quinze verificações em sequência, incluindo os caminhos negativos que a interface não alcança:
compra duplicada recusada com `409`, webhook repetido sem efeito, código de pagamento inexistente
com `404`, veículo vendido que não pode ser editado, e as rotas internas exigindo token. Termina
com **"All 15 steps passed."**

> Se for rodar o script, rode-o **depois** da demonstração na interface — ele cria dados próprios
> e deixaria a vitrine com mais itens.

---

## Bloco 5 — Testes rodando (~2 min)

```bash
cd core && dotnet test --solution FiapSub.Core.slnx
```

Termine com **`total: 177 · falhou: 0`** na tela.

```bash
cd ../sales && dotnet test --solution FiapSub.Sales.slnx
```

Termine com **`total: 197 · falhou: 0`**.

> Fala: *"Testes de domínio para as invariantes dos agregados, de aplicação para os casos de uso,
> e de integração que sobem um PostgreSQL de verdade com Testcontainers e aplicam as migrations."*

---

## Bloco 6 — Evidências da cobertura (~2 min)

O `reportgenerator` é uma ferramenta global do .NET. Se o comando não for encontrado, falta o
diretório de ferramentas no `PATH` — resolva antes de gravar:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool   # se ainda não tiver
fish_add_path ~/.dotnet/tools                                    # fish; no bash: export PATH="$PATH:$HOME/.dotnet/tools"
reportgenerator --version                                        # confirme
```

```bash
cd ../core

rm -rf TestResults   # cada execução gera relatórios novos; comece limpo
dotnet test --solution FiapSub.Core.slnx \
  --coverage --coverage-settings coverage.runsettings \
  --results-directory TestResults --coverage-output-format cobertura

reportgenerator -reports:"TestResults/*.cobertura.xml" \
  -targetdir:coveragereport -reporttypes:"Html;TextSummary"

cat coveragereport/Summary.txt | head -12
```

Mostre a linha **`Line coverage: 99.8%`** e, abaixo, cada assembly.

```bash
xdg-open coveragereport/index.html
```

Abra o relatório HTML e navegue por um ou dois arquivos, mostrando as linhas cobertas.

Repita no `sales` (**100%**):

```bash
cd ../sales

rm -rf TestResults   # cada execução gera relatórios novos; comece limpo
dotnet test --solution FiapSub.Sales.slnx \
  --coverage --coverage-settings coverage.runsettings \
  --results-directory TestResults --coverage-output-format cobertura

reportgenerator -reports:"TestResults/*.cobertura.xml" \
  -targetdir:coveragereport -reporttypes:"Html;TextSummary"

cat coveragereport/Summary.txt | head -12
```

> Fala: *"O mínimo exigido é 80%. O pipeline falha abaixo disso — não é só um número no relatório."*

---

## Bloco 7 — CI/CD (~2 min)

Mostre o workflow e o gate:

```bash
sed -n '1,40p' .github/workflows/sales.yml
grep -n 'MIN_LINE_COVERAGE\|Coverage gate failed\|refs/heads/main' .github/workflows/sales.yml
```

> Fala: *"Em pull request, roda build, testes e o gate de 80%. No merge na main, roda o mesmo gate,
> publica a imagem no GHCR e faz o deploy no Kubernetes: aplica os manifests, espera o
> `rollout status` e roda um smoke test. Sem cluster próprio configurado, o job sobe um cluster kind
> no runner — o deploy nunca é pulado."*

No GitHub, mostre:

1. Um **Pull Request** com o check **Build and test** verde.
2. A aba **Actions** do run do merge na `main`, com o job **Publish and deploy** verde e o resumo
   *Deployment* dizendo em qual cluster a imagem foi publicada.
3. O artefato **`<serviço>-coverage-report`** disponível para download no run — é a evidência de
   cobertura gerada pelo próprio pipeline.
4. Em *Settings → Branches*, a regra que exige PR e o check aprovado na `main`.

---

## Checklist final antes de publicar o vídeo

- [ ] Os cinco containers apareceram `healthy`
- [ ] Os dois `psql` mostraram tabelas **diferentes** em cada banco
- [ ] Cadastro dos três veículos, fora da ordem de preço
- [ ] Listagem **à venda** ordenada do mais barato para o mais caro
- [ ] Edição de veículo refletida na listagem
- [ ] Compra com CPF, devolvendo o código do pagamento
- [ ] Webhook aprovando o pagamento e efetivando a venda
- [ ] Listagem **vendidos** ordenada por preço
- [ ] Data da venda: automática em um caso, informada em outro
- [ ] Pagamento cancelado devolvendo o veículo à vitrine
- [ ] Logs mostrando a comunicação HTTP nos dois sentidos
- [ ] Rotas internas recusando chamada sem token
- [ ] `177` e `197` testes passando, na tela
- [ ] Cobertura `99,8%` e `100%`, com o relatório HTML aberto
- [ ] Os manifests rodando no kind: pods `Running` e o `e2e.sh` verde pelos `Service`s
- [ ] Pipeline: PR verde, deploy no merge, artefato de cobertura
