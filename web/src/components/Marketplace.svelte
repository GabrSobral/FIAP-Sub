<script>
  import BuyForm from './BuyForm.svelte';
  import StatusChip from './StatusChip.svelte';
  import { sales, describe } from '../lib/api.js';
  import { brl, at } from '../lib/format.js';
  import { toasts } from '../lib/toasts.svelte.js';

  let { ongotopayments } = $props();

  let tab = $state('for-sale');
  let page = $state({ items: [], totalCount: 0 });
  let loading = $state(true);
  let failure = $state(null);
  let buying = $state(null);

  async function load() {
    loading = true;
    failure = null;

    try {
      const query = { page: 1, pageSize: 100 };

      page = tab === 'for-sale' ? await sales.listForSale(query) : await sales.listSold(query);
    } catch (error) {
      failure = describe(error);
      page = { items: [], totalCount: 0 };
    } finally {
      loading = false;
    }
  }

  // Re-runs whenever the tab changes.
  $effect(() => {
    tab;
    load();
  });

  function bought(sale) {
    buying = null;
    load();
    toasts.info('O veículo saiu da vitrine e aguarda o pagamento.');
    ongotopayments?.(sale);
  }

  const columns = $derived(tab === 'for-sale' ? 'À venda' : 'Vendidos');
</script>

<section class="stack">
  <div class="panel">
    <div class="panel-head">
      <div>
        <h2>Vitrine</h2>
        <p class="small muted">
          Servido pelo serviço de vendas, do próprio banco dele, ordenado por preço — do mais
          barato para o mais caro.
        </p>
      </div>

      <div class="row">
        <div class="tabs" role="tablist" aria-label="Listagens">
          <button
            type="button"
            role="tab"
            aria-selected={tab === 'for-sale'}
            class:active={tab === 'for-sale'}
            onclick={() => (tab = 'for-sale')}
          >
            À venda
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={tab === 'sold'}
            class:active={tab === 'sold'}
            onclick={() => (tab = 'sold')}
          >
            Vendidos
          </button>
        </div>

        <button type="button" class="btn btn-sm" onclick={load} disabled={loading}>
          {#if loading}<span class="spinner"></span>{/if}
          Atualizar
        </button>
      </div>
    </div>

    <div class="panel-body tight">
      {#if failure}
        <div class="empty">
          <strong>O serviço de vendas não respondeu</strong>
          <p class="small">{failure}</p>
        </div>
      {:else if loading && page.items.length === 0}
        <div class="empty"><span class="spinner"></span></div>
      {:else if page.items.length === 0}
        <div class="empty">
          <strong>Nenhum veículo em “{columns}”</strong>
          {#if tab === 'for-sale'}
            <p class="small">
              A vitrine é alimentada pelos pushes do serviço core. Cadastre um veículo na aba
              Catálogo — ou reconcilie a projeção inteira:
            </p>
            <p class="small mono sync">
              curl -X POST localhost:8081/api/internal/catalogue/sync -H
              'X-Internal-Token: local-dev-internal-token'
            </p>
          {:else}
            <p class="small">Nenhuma venda foi concluída ainda.</p>
          {/if}
        </div>
      {:else}
        <ol class="listing">
          {#each page.items as vehicle, index (vehicle.vehicleId)}
            <li>
              <span class="rank tnum faint">{index + 1}</span>

              <div class="grow">
                <div class="row" style="gap: 8px">
                  <strong class="truncate">{vehicle.brand} {vehicle.model}</strong>
                  <StatusChip status={vehicle.status} />
                </div>
                <p class="small muted">
                  {vehicle.year} · {vehicle.color} ·
                  <span class="mono" title={vehicle.vehicleId}>{vehicle.vehicleId.slice(0, 8)}</span>
                  · atualizado {at(vehicle.updatedAt)}
                </p>
              </div>

              <span class="price tnum">{brl(vehicle.price, vehicle.currency)}</span>

              {#if tab === 'for-sale'}
                <button type="button" class="btn btn-primary btn-sm" onclick={() => (buying = vehicle)}>
                  Comprar
                </button>
              {/if}
            </li>
          {/each}
        </ol>

        <footer class="count small muted">
          {page.totalCount} {page.totalCount === 1 ? 'veículo' : 'veículos'} em “{columns}”
        </footer>
      {/if}
    </div>
  </div>
</section>

{#if buying}
  <BuyForm vehicle={buying} onclose={() => (buying = null)} onbought={bought} />
{/if}

<style>
  .tabs {
    display: inline-flex;
    padding: 2px;
    gap: 2px;
    background: var(--surface-3);
    border: 1px solid var(--border);
    border-radius: var(--radius-sm);
  }

  .tabs button {
    appearance: none;
    border: none;
    background: transparent;
    color: var(--text-muted);
    font: inherit;
    font-size: 0.8125rem;
    font-weight: 550;
    padding: 5px 12px;
    border-radius: 4px;
    cursor: pointer;
  }

  .tabs button.active {
    background: var(--surface);
    color: var(--text);
    box-shadow: var(--shadow);
  }

  .listing {
    list-style: none;
    margin: 0;
    padding: 0;
  }

  .listing li {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 13px 18px;
    border-bottom: 1px solid var(--border);
  }

  .listing li:last-child {
    border-bottom: none;
  }

  .listing li:hover {
    background: var(--surface-2);
  }

  /* The ordinal makes the price ordering visible at a glance, not just implied. */
  .rank {
    width: 1.5rem;
    text-align: right;
    font-size: 0.8125rem;
    flex: none;
  }

  .price {
    font-size: 1.05rem;
    font-weight: 650;
    color: var(--accent);
    white-space: nowrap;
  }

  .count {
    padding: 11px 18px;
    border-top: 1px solid var(--border);
    background: var(--surface-2);
    border-radius: 0 0 var(--radius) var(--radius);
  }

  .sync {
    display: block;
    margin-top: 10px;
    padding: 8px 10px;
    background: var(--surface-2);
    border: 1px solid var(--border);
    border-radius: var(--radius-sm);
    text-align: left;
    word-break: break-all;
  }

  @media (max-width: 560px) {
    .listing li {
      flex-wrap: wrap;
    }

    .rank {
      display: none;
    }
  }
</style>
