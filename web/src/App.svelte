<script>
  import Marketplace from './components/Marketplace.svelte';
  import Catalogue from './components/Catalogue.svelte';
  import Payments from './components/Payments.svelte';
  import Toasts from './components/Toasts.svelte';
  import { core, sales } from './lib/api.js';

  const TABS = [
    { id: 'market', label: 'Vitrine', hint: 'Listagens e compra — serviço de vendas' },
    { id: 'catalogue', label: 'Catálogo', hint: 'Cadastro e edição — serviço core' },
    { id: 'payments', label: 'Pagamentos', hint: 'Webhook do provedor — serviço core' },
  ];

  let tab = $state('market');
  let health = $state({ core: 'checking', sales: 'checking' });

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

  // Each service reports its own readiness, which includes its own database.
  $effect(() => {
    probe();

    const timer = setInterval(probe, 10000);

    return () => clearInterval(timer);
  });

  const tone = (state) =>
    state === 'up' ? 'chip-ok' : state === 'down' ? 'chip-danger' : 'chip-neutral';

  const label = (state) => (state === 'up' ? 'no ar' : state === 'down' ? 'fora' : '…');
</script>

<header>
  <div class="bar">
    <div class="brand">
      <span class="mark" aria-hidden="true">🚗</span>
      <div>
        <h1>Revenda de Veículos</h1>
        <p class="small faint">FIAP SOAT · Pós-Tech — Fase 4</p>
      </div>
    </div>

    <div class="row" aria-label="Saúde dos serviços">
      <span class="chip {tone(health.core)}">core {label(health.core)}</span>
      <span class="chip {tone(health.sales)}">vendas {label(health.sales)}</span>
    </div>
  </div>

  <nav aria-label="Seções">
    {#each TABS as item (item.id)}
      <button
        type="button"
        class:active={tab === item.id}
        aria-current={tab === item.id ? 'page' : undefined}
        title={item.hint}
        onclick={() => (tab = item.id)}
      >
        {item.label}
      </button>
    {/each}
  </nav>
</header>

<main class="app">
  {#if health.core === 'down' && health.sales === 'down'}
    <div class="panel offline">
      <div class="panel-body">
        <strong>Nenhum dos dois serviços respondeu.</strong>
        <p class="small muted">
          Suba a stack na raiz do repositório com <code>docker compose up --build</code> e esta
          página se reconecta sozinha.
        </p>
      </div>
    </div>
  {/if}

  {#if tab === 'market'}
    <Marketplace ongotopayments={() => (tab = 'payments')} />
  {:else if tab === 'catalogue'}
    <Catalogue />
  {:else}
    <Payments />
  {/if}
</main>

<Toasts />

<style>
  header {
    position: sticky;
    top: 0;
    z-index: 10;
    background: color-mix(in srgb, var(--bg) 88%, transparent);
    backdrop-filter: blur(8px);
    border-bottom: 1px solid var(--border);
  }

  .bar,
  nav {
    max-width: 1120px;
    margin: 0 auto;
    padding: 0 20px;
  }

  .bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    flex-wrap: wrap;
    padding-top: 14px;
    padding-bottom: 12px;
  }

  .brand {
    display: flex;
    align-items: center;
    gap: 11px;
  }

  .mark {
    display: grid;
    place-items: center;
    width: 34px;
    height: 34px;
    border-radius: 9px;
    background: var(--accent-soft);
    border: 1px solid color-mix(in srgb, var(--accent) 28%, transparent);
    font-size: 17px;
    flex: none;
  }

  nav {
    display: flex;
    gap: 4px;
    overflow-x: auto;
  }

  nav button {
    appearance: none;
    border: none;
    background: none;
    font: inherit;
    font-size: 0.9rem;
    font-weight: 550;
    color: var(--text-muted);
    padding: 9px 12px;
    /* The active underline sits on the header border, so the tab reads as attached to the page. */
    border-bottom: 2px solid transparent;
    margin-bottom: -1px;
    cursor: pointer;
    white-space: nowrap;
  }

  nav button:hover {
    color: var(--text);
  }

  nav button.active {
    color: var(--accent);
    border-bottom-color: var(--accent);
  }

  main {
    padding-top: 22px;
  }

  .offline {
    margin-bottom: 14px;
    border-left: 3px solid var(--danger);
  }
</style>
