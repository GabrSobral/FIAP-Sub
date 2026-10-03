# Interface Web — Revenda de Veículos

Painel que consome os dois serviços da plataforma. Escrito em **Svelte 5 com JavaScript e CSS à
mão**: nenhum framework de CSS, nenhum TypeScript, nenhuma biblioteca de componentes. Vite só faz
o papel de bundler.

> Esta interface é um **extra**. O enunciado entrega a API para que o time de frontend integre
> depois; aqui ela existe para demonstrar a solução funcionando ponta a ponta no navegador, o que
> é exatamente o que o vídeo precisa mostrar.

## O que a tela faz

| Aba | O que exercita | Serviço |
|---|---|---|
| **Vitrine** | Veículos **à venda** e **vendidos**, ordenados por preço (do mais barato para o mais caro), com a posição numerada para o ordenamento ficar visível. Botão de comprar, com CPF e data da venda opcional. | `sales` |
| **Catálogo** | Cadastro e edição de veículo (marca, modelo, ano, cor, preço), com filtro por status e a versão de cada agregado. | `core` |
| **Pagamentos** | Faz o papel do **provedor de pagamento**: chama o webhook real de aprovação/cancelamento a partir do código do pagamento. Também consulta uma venda por id. | `core` + `sales` |

No cabeçalho, duas pastilhas mostram a saúde de cada serviço (`/health/ready`, que inclui o banco
de cada um), revalidadas a cada 10 segundos.

## Como rodar

### Junto com a stack completa (recomendado)

Na raiz do repositório:

```bash
docker compose up --build
```

A interface fica em <http://localhost:3000>.

### Em modo de desenvolvimento

```bash
docker compose up -d core-db core-api sales-db sales-api   # as APIs
cd web
npm install
npm run dev                                                # http://localhost:5173
```

Para apontar para APIs em outros endereços:

```bash
CORE_API_URL=http://localhost:9000 SALES_API_URL=http://localhost:9001 npm run dev
```

## Nada de CORS, por desenho

As duas APIs são alcançadas por prefixos **do mesmo origin** — `/core-api` e `/sales-api` — que um
proxy reescreve:

- `npm run dev` → o proxy do Vite (`vite.config.js`);
- container → nginx (`nginx.conf`).

O navegador nunca faz requisição cross-origin, então nenhum preflight é emitido e **nenhum dos dois
serviços precisa de CORS habilitado**. As URLs são idênticas em dev e em produção, então não existe
caminho que funcione só num dos dois.

O nginx também devolve `404` para qualquer coisa sob `/api/internal`: as rotas entre serviços não
são assunto do navegador, e o token compartilhado nunca chega ao cliente.

## Decisões que valem explicação

**A lista de pagamentos é memória local.** A API de vendas expõe uma venda apenas por id
(`GET /api/sales/{id}`) — não existe endpoint de listagem, e criar um estava fora do escopo do
enunciado. Então esta tela guarda no `localStorage` as compras que **ela própria** abriu e relê
cada uma da API para mostrar o estado atual. O botão *Reler vendas* faz esse refresh.

**O CPF é validado aqui também.** O mesmo algoritmo de dígitos verificadores do value object
`Cpf` roda no navegador, para dar retorno imediato a quem digita. É gentileza, não fronteira de
segurança: o domínio continua tendo a palavra final, e a tela mostra o erro que vier de lá.

**A vitrine pode começar vazia.** Ela é alimentada pelos pushes do serviço core. Se o `sales`
subiu depois de o `core` já ter cadastrado veículos, o estado vazio mostra o comando de
reconciliação — que precisa do token interno e por isso roda no terminal, não no navegador.

**Editar só vale para veículo à venda.** O botão fica desabilitado nos outros status, com o motivo
no `title`, espelhando a regra do agregado em vez de deixar o usuário descobrir pelo 409.

## Estrutura

```
web/
├── index.html              único HTML da aplicação
├── vite.config.js          bundler + proxy das duas APIs em dev
├── nginx.conf              serve o SPA e faz o mesmo proxy no container
├── Dockerfile              build com node, runtime em nginx
└── src/
    ├── main.js             ponto de entrada
    ├── app.css             design tokens e estilos globais, escritos à mão
    ├── App.svelte          cabeçalho, navegação e saúde dos serviços
    ├── lib/
    │   ├── api.js          cliente das duas APIs, com ApiError sobre ProblemDetails
    │   ├── format.js       moeda, datas, máscara e validação de CPF
    │   ├── toasts.svelte.js    notificações
    │   └── sale-log.svelte.js  vendas abertas neste navegador
    └── components/
        ├── Marketplace.svelte  vitrine: à venda / vendidos / comprar
        ├── BuyForm.svelte      formulário de compra
        ├── Catalogue.svelte    cadastro e edição
        ├── VehicleForm.svelte  formulário do veículo
        ├── Payments.svelte     simulador do provedor + consulta de venda
        ├── StatusChip.svelte   pastilha de status
        ├── Modal.svelte        diálogo sobre <dialog> nativo
        └── Toasts.svelte       pilha de notificações
```

## Detalhes de interface

- **Tema claro e escuro** por `prefers-color-scheme`, com a paleta clara completa em `:root` e o
  modo escuro apenas redefinindo os tokens que mudam.
- **Erros são legíveis**: o `ProblemDetails` das APIs é desempacotado — `detail` vira a mensagem, e
  `errors` por campo vira marcação no próprio input.
- **Preços e ids com algarismos tabulares**, para as colunas alinharem.
- `<dialog>` nativo nos modais, o que traz foco preso, backdrop e `Esc` sem JavaScript extra.
- `prefers-reduced-motion` respeitado.
- Falhas ficam na tela até serem fechadas; sucessos desaparecem sozinhos.

## Build de produção

```bash
npm run build      # gera dist/
npm run preview    # serve dist/ em http://localhost:4173
```

O bundle fica em torno de **26 kB gzip** (JS + CSS), sem dependências de runtime além do próprio
Svelte compilado.
