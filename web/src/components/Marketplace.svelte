<script>
  import BuyForm from './BuyForm.svelte';
  import StatusChip from './StatusChip.svelte';
  import Figure from './Figure.svelte';
  import Icon from './Icon.svelte';
  import { sales, describe } from '../lib/api.js';
  import { ago } from '../lib/format.js';
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

  // Roda de novo sempre que a aba muda.
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

  const which = $derived(tab === 'for-sale' ? 'à venda' : 'vendidos');
</script>

<section class="stack">
  <header class="page">
    <div class="grow">
      <h2>Vitrine</h2>
      <p class="lede">
        O serviço de vendas responde do banco dele, sem consultar o core. Do mais barato ao mais
        caro.
      </p>
    </div>

    <div class="row">
      <div class="switch" role="tablist" aria-label="Listagens">
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'for-sale'}
          class:on={tab === 'for-sale'}
          onclick={() => (tab = 'for-sale')}
        >
          À venda
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'sold'}
          class:on={tab === 'sold'}
          onclick={() => (tab = 'sold')}
        >
          Vendidos
        </button>
      </div>

      <button type="button" class="btn btn-sm" onclick={load} disabled={loading}>
        {#if loading}<span class="spinner"></span>{:else}<Icon name="refresh" size={14} />{/if}
        Atualizar
      </button>
    </div>
  </header>

  <div class="sheet">
    <div class="sheet-body flush">
      {#if failure}
        <div class="blank">
          <span class="blank-mark"><Icon name="close" /></span>
          <div>
            <strong>O serviço de vendas não respondeu.</strong>
            <p>{failure}</p>
          </div>
        </div>
      {:else if loading && page.items.length === 0}
        <div class="blank">
          <span class="blank-mark"><span class="spinner"></span></span>
          <p>Lendo a listagem…</p>
        </div>
      {:else if page.items.length === 0}
        <div class="blank">
          <span class="blank-mark"><Icon name="inbox" /></span>
          <div>
            <strong>Nada {which} por aqui.</strong>
            {#if tab === 'for-sale'}
              <p>
                A vitrine é alimentada pelos pushes do core. Cadastre um veículo no Catálogo — ou,
                se a projeção ficou atrás, reconcilie tudo de uma vez:
              </p>
              <p class="sync mono">
                curl -X POST localhost:8081/api/internal/catalogue/sync -H 'X-Internal-Token:
                local-dev-internal-token'
              </p>
            {:else}
              <p>Nenhuma venda foi concluída ainda.</p>
            {/if}
          </div>
        </div>
      {:else}
        <ol class="ladder">
          {#each page.items as vehicle, index (vehicle.vehicleId)}
            <li>
              <span class="pos">{String(index + 1).padStart(2, '0')}</span>

              <div class="grow">
                <div class="name">
                  <strong class="truncate">{vehicle.brand} {vehicle.model}</strong>
                  <StatusChip status={vehicle.status} />
                </div>
                <p class="meta">
                  {vehicle.year} · {vehicle.color} ·
                  <span class="mono" title={vehicle.vehicleId}>{vehicle.vehicleId.slice(0, 8)}</span>
                  · {ago(vehicle.updatedAt)}
                </p>
              </div>

              <Figure amount={vehicle.price} currency={vehicle.currency} large />

              {#if tab === 'for-sale'}
                <button type="button" class="btn btn-sm buy" onclick={() => (buying = vehicle)}>
                  Comprar
                </button>
              {/if}
            </li>
          {/each}
        </ol>

        <p class="tally">
          {page.totalCount}
          {page.totalCount === 1 ? 'veículo' : 'veículos'}
          {which}
        </p>
      {/if}
    </div>
  </div>
</section>

{#if buying}
  <BuyForm vehicle={buying} onclose={() => (buying = null)} onbought={bought} />
{/if}

<style>
  /* Duas listagens, um controle: as abas ficam no cabeçalho da seção, não numa
     barra própria, para não haver dois níveis de navegação na tela. */
  .switch {
    display: inline-flex;
    border: 1px solid var(--rule-strong);
    border-radius: var(--corner);
    overflow: hidden;
  }

  .switch button {
    appearance: none;
    border: none;
    border-left: 1px solid var(--rule-strong);
    background: var(--sheet);
    color: var(--ink-2);
    font: inherit;
    font-size: 0.8125rem;
    font-weight: 560;
    padding: 6px 13px;
    cursor: pointer;
  }

  .switch button:first-child {
    border-left: none;
  }

  .switch button:hover:not(.on) {
    background: var(--sheet-2);
    color: var(--ink);
  }

  .switch button.on {
    background: var(--accent);
    color: var(--accent-ink);
  }

  /* Neutro em repouso, azul sob o cursor: a ação se anuncia na linha que o
     olho já está lendo, sem três blocos saturados competindo com os preços. */
  .buy:hover:not(:disabled) {
    background: var(--accent);
    border-color: var(--accent);
    color: var(--accent-ink);
  }

  .sync {
    display: block;
    margin-top: 9px;
    padding: 8px 10px;
    background: var(--sheet-2);
    border: 1px solid var(--rule);
    border-left: 2px solid var(--rule-strong);
    font-size: 0.75rem;
    color: var(--ink-2);
    word-break: break-all;
  }

  @media (max-width: 620px) {
    :global(.ladder li) {
      flex-wrap: wrap;
    }

    :global(.ladder .pos) {
      display: none;
    }
  }
</style>
