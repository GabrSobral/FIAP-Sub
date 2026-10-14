<script>
  import Marketplace from './components/Marketplace.svelte';
  import Catalogue from './components/Catalogue.svelte';
  import Payments from './components/Payments.svelte';
  import Toasts from './components/Toasts.svelte';
  import Icon from './components/Icon.svelte';
  import { core, sales } from './lib/api.js';

  const TABS = [
    { id: 'market', icon: 'tag', label: 'Vitrine', sub: 'serviço de vendas' },
    { id: 'catalogue', icon: 'sheet', label: 'Catálogo', sub: 'serviço core' },
    { id: 'payments', icon: 'receipt', label: 'Pagamentos', sub: 'webhook do core' },
  ];

  const SERVICES = [
    { key: 'core', name: 'core', port: '8080' },
    { key: 'sales', name: 'vendas', port: '8081' },
  ];

  // A seção fica no hash: recarregar não volta para a primeira, e cada uma tem
  // link próprio — #catalogue, #payments.
  function fromHash() {
    const id = location.hash.slice(1);

    return TABS.some((item) => item.id === id) ? id : TABS[0].id;
  }

  let tab = $state(fromHash());
  let health = $state({ core: 'checking', sales: 'checking' });

  function go(id) {
    tab = id;

    if (location.hash.slice(1) !== id) history.replaceState(null, '', `#${id}`);
  }

  async function probe() {
    const check = async (service) => {
      try {
        await service.ready();
        return 'up';
      } catch {
        return 'down';
      }
    };

    const [coreState, salesState] = await Promise.all([check(core), check(sales)]);

    health = { core: coreState, sales: salesState };
  }

  // Cada serviço responde pela própria prontidão, banco incluído.
  $effect(() => {
    probe();

    const timer = setInterval(probe, 10000);

    return () => clearInterval(timer);
  });

  // 1, 2, 3 trocam de seção. Numa demonstração gravada vale não caçar o mouse.
  function onKey(event) {
    if (event.metaKey || event.ctrlKey || event.altKey) return;

    const typing = event.target?.closest?.('input, select, textarea, dialog');

    if (typing) return;

    const index = Number(event.key) - 1;

    if (index >= 0 && index < TABS.length) go(TABS[index].id);
  }

  const label = (state) => (state === 'up' ? 'no ar' : state === 'down' ? 'sem resposta' : '…');
</script>

<svelte:window onkeydown={onKey} onhashchange={() => (tab = fromHash())} />

<div class="shell">
  <aside class="board">
    <div class="mark">
      <!-- Uma placa, reduzida ao essencial: moldura, tarja e os caracteres. -->
      <svg viewBox="0 0 30 21" width="30" height="21" aria-hidden="true">
        <rect x="1" y="1" width="28" height="19" rx="2.5" fill="none" stroke="currentColor" stroke-width="1.5" />
        <path d="M1.4 7h27.2" stroke="currentColor" stroke-width="1.5" />
        <path
          d="M6 13.4h3.4M11.8 13.4h2.6M16.8 13.4h4.2M23.4 13.4h.8"
          stroke="currentColor"
          stroke-width="1.8"
          stroke-linecap="round"
          opacity="0.75"
        />
      </svg>

      <div>
        <h1>Pátio</h1>
        <p>Revenda de veículos<br />FIAP SOAT · Fase 4</p>
      </div>
    </div>

    <nav aria-label="Seções">
      {#each TABS as item, index (item.id)}
        <button
          type="button"
          class:active={tab === item.id}
          aria-current={tab === item.id ? 'page' : undefined}
          title="Atalho: {index + 1}"
          onclick={() => go(item.id)}
        >
          <Icon name={item.icon} />
          <span class="grow">
            {item.label}
            <em>{item.sub}</em>
          </span>
          <span class="key" aria-hidden="true">{index + 1}</span>
        </button>
      {/each}
    </nav>

    <div class="board-foot">
      {#each SERVICES as service (service.key)}
        <p
          class="probe"
          class:up={health[service.key] === 'up'}
          class:down={health[service.key] === 'down'}
          title="{service.name}: {label(health[service.key])}"
        >
          <span class="dot" aria-hidden="true"></span>
          <span class="grow">{service.name}</span>
          <span class="port mono">:{service.port}</span>
          <span class="sr-only">{label(health[service.key])}</span>
        </p>
      {/each}
      <p class="probe-note">/health/ready a cada 10 s</p>
    </div>
  </aside>

  <main>
    {#if health.core === 'down' && health.sales === 'down'}
      <div class="halt">
        <Icon name="alert" size={16} />
        <div>
          <strong>Nenhum dos dois serviços respondeu.</strong>
          <p>
            Suba a stack na raiz do repositório com <code>docker compose up --build</code>. Esta
            página se reconecta sozinha, a cada dez segundos.
          </p>
        </div>
      </div>
    {/if}

    {#if tab === 'market'}
      <Marketplace ongotopayments={() => go('payments')} />
    {:else if tab === 'catalogue'}
      <Catalogue />
    {:else}
      <Payments />
    {/if}
  </main>
</div>

<Toasts />

<style>
  .shell {
    display: grid;
    grid-template-columns: 236px minmax(0, 1fr);
    min-height: 100vh;
  }

  /* O rail é um bloco de tinta, do jeito que a placa de um pátio é. */
  .board {
    position: sticky;
    top: 0;
    align-self: start;
    height: 100vh;
    display: flex;
    flex-direction: column;
    background: var(--board);
    color: var(--board-text);
    border-right: 1px solid var(--board-rule);
  }

  .mark {
    display: flex;
    align-items: center;
    gap: 11px;
    padding: 20px 18px 18px;
    color: var(--board-accent);
  }

  .mark h1 {
    color: var(--board-text);
    font-size: 1.1875rem;
    font-weight: 740;
    letter-spacing: -0.03em;
  }

  .mark p {
    color: var(--board-text-2);
    font-size: 0.6875rem;
    line-height: 1.35;
    margin-top: 2px;
  }

  nav {
    display: flex;
    flex-direction: column;
    padding: 6px 8px;
    gap: 1px;
  }

  nav button {
    display: flex;
    align-items: center;
    gap: 10px;
    width: 100%;
    appearance: none;
    border: none;
    border-radius: var(--corner);
    background: none;
    color: var(--board-text-2);
    font: inherit;
    font-size: 0.875rem;
    font-weight: 560;
    text-align: left;
    padding: 9px 10px;
    cursor: pointer;
    transition: background-color 110ms ease, color 110ms ease;
  }

  nav button em {
    display: block;
    font-style: normal;
    font-size: 0.6875rem;
    font-weight: 440;
    color: var(--board-text-2);
    opacity: 0.72;
  }

  nav button:hover {
    background: var(--board-2);
    color: var(--board-text);
  }

  nav button.active {
    background: var(--board-2);
    color: var(--board-text);
    /* Marca de seleção na borda interna, como um clipe segurando a ficha. */
    box-shadow: inset 2px 0 0 var(--board-accent);
  }

  nav button.active em {
    color: var(--board-accent);
    opacity: 1;
  }

  .key {
    font-family: var(--mono);
    font-size: 0.6875rem;
    color: var(--board-text-2);
    opacity: 0.5;
  }

  .board-foot {
    margin-top: auto;
    padding: 14px 18px 16px;
    border-top: 1px solid var(--board-rule);
    display: flex;
    flex-direction: column;
    gap: 5px;
  }

  .probe {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 0.78125rem;
    color: var(--board-text-2);
  }

  .dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: var(--board-text-2);
    flex: none;
  }

  .probe.up .dot {
    background: #63bf8d;
  }

  .probe.down .dot {
    background: #e4867f;
  }

  .probe.up {
    color: var(--board-text);
  }

  .port {
    opacity: 0.6;
  }

  .probe-note {
    font-size: 0.6875rem;
    color: var(--board-text-2);
    opacity: 0.6;
    margin-top: 2px;
  }

  main {
    padding: 26px 28px 72px;
    max-width: 1180px;
  }

  .halt {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    margin-bottom: 18px;
    padding: 13px 15px;
    background: var(--stop-wash);
    border: 1px solid color-mix(in srgb, var(--stop) 35%, transparent);
    border-left-width: 3px;
    border-radius: var(--corner);
    color: var(--stop);
    font-size: 0.875rem;
  }

  .halt strong {
    font-weight: 620;
  }

  .halt p {
    color: var(--ink-2);
    max-width: 68ch;
  }

  @media (max-width: 880px) {
    .shell {
      grid-template-columns: 1fr;
    }

    .board {
      position: static;
      height: auto;
      border-right: none;
      border-bottom: 1px solid var(--board-rule);
    }

    nav {
      flex-direction: row;
      overflow-x: auto;
    }

    nav button em,
    .key {
      display: none;
    }

    .board-foot {
      flex-direction: row;
      align-items: center;
      gap: 14px;
      padding: 10px 18px;
    }

    .probe-note {
      display: none;
    }

    main {
      padding: 20px 18px 64px;
    }
  }
</style>
