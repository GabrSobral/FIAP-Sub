<script>
  import StatusChip from './StatusChip.svelte';
  import VehicleForm from './VehicleForm.svelte';
  import Figure from './Figure.svelte';
  import Icon from './Icon.svelte';
  import { core, describe } from '../lib/api.js';
  import { at } from '../lib/format.js';

  let page = $state({ items: [], totalCount: 0 });
  let status = $state('');
  let loading = $state(true);
  let failure = $state(null);
  let editor = $state(null); // { vehicle: null } = cadastrar, { vehicle } = editar
  let copied = $state(null);

  async function load() {
    loading = true;
    failure = null;

    try {
      page = await core.listVehicles({ status: status || undefined, page: 1, pageSize: 100 });
    } catch (error) {
      failure = describe(error);
      page = { items: [], totalCount: 0 };
    } finally {
      loading = false;
    }
  }

  $effect(() => {
    status;
    load();
  });

  function saved() {
    editor = null;
    load();
  }

  async function copy(id) {
    try {
      await navigator.clipboard.writeText(id);
      copied = id;
      setTimeout(() => (copied = copied === id ? null : copied), 1400);
    } catch {
      // O navegador pode negar a área de transferência; o id inteiro está no title.
    }
  }
</script>

<section class="stack">
  <header class="page">
    <div class="grow">
      <h2>Catálogo</h2>
      <p class="lede">
        O core é a única fonte de verdade do estado de um veículo. Toda mudança daqui sai por HTTP
        para o serviço de vendas.
      </p>
    </div>

    <div class="row">
      <label class="sr-only" for="filter-status">Filtrar por status</label>
      <select id="filter-status" bind:value={status}>
        <option value="">Todos os status</option>
        <option value="Available">À venda</option>
        <option value="Reserved">Reservados</option>
        <option value="Sold">Vendidos</option>
      </select>

      <button type="button" class="btn btn-sm" onclick={load} disabled={loading}>
        {#if loading}<span class="spinner"></span>{:else}<Icon name="refresh" size={14} />{/if}
        Atualizar
      </button>

      <button
        type="button"
        class="btn btn-primary btn-sm"
        onclick={() => (editor = { vehicle: null })}
      >
        <Icon name="plus" size={14} />
        Cadastrar veículo
      </button>
    </div>
  </header>

  <div class="sheet">
    <div class="sheet-body flush">
      {#if failure}
        <div class="blank">
          <span class="blank-mark"><Icon name="close" /></span>
          <div>
            <strong>O serviço core não respondeu.</strong>
            <p>{failure}</p>
          </div>
        </div>
      {:else if loading && page.items.length === 0}
        <div class="blank">
          <span class="blank-mark"><span class="spinner"></span></span>
          <p>Lendo o catálogo…</p>
        </div>
      {:else if page.items.length === 0}
        <div class="blank">
          <span class="blank-mark"><Icon name="car" /></span>
          <div>
            <strong>Catálogo vazio.</strong>
            <p>Cadastre o primeiro veículo e ele aparece na vitrine em seguida.</p>
          </div>
        </div>
      {:else}
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Veículo</th>
                <th>Ano</th>
                <th>Cor</th>
                <th class="num">Preço</th>
                <th>Status</th>
                <th>Código do pagamento</th>
                <th class="num">Versão</th>
                <th>Atualizado</th>
                <th><span class="sr-only">Ações</span></th>
              </tr>
            </thead>
            <tbody>
              {#each page.items as vehicle (vehicle.id)}
                <tr>
                  <td>
                    <strong>{vehicle.brand} {vehicle.model}</strong>
                    <button
                      type="button"
                      class="id"
                      title="Copiar {vehicle.id}"
                      onclick={() => copy(vehicle.id)}
                    >
                      {#if copied === vehicle.id}
                        <Icon name="check" size={11} /> copiado
                      {:else}
                        <Icon name="copy" size={11} /> {vehicle.id.slice(0, 8)}
                      {/if}
                    </button>
                  </td>
                  <td class="tnum">{vehicle.year}</td>
                  <td>{vehicle.color}</td>
                  <td class="num"><Figure amount={vehicle.price} currency={vehicle.currency} /></td>
                  <td><StatusChip status={vehicle.status} /></td>
                  <td class="mono faint">{vehicle.paymentCode ?? '—'}</td>
                  <td class="num">{vehicle.version}</td>
                  <td class="small muted nowrap">{at(vehicle.updatedAt)}</td>
                  <td>
                    <button
                      type="button"
                      class="btn btn-sm"
                      disabled={vehicle.status !== 'Available'}
                      title={vehicle.status === 'Available'
                        ? 'Editar'
                        : 'Só um veículo à venda pode ser editado'}
                      onclick={() => (editor = { vehicle })}
                    >
                      Editar
                    </button>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        <p class="tally">
          {page.totalCount}
          {page.totalCount === 1 ? 'veículo' : 'veículos'}
          {status ? 'neste filtro' : 'no catálogo'}
        </p>
      {/if}
    </div>
  </div>
</section>

{#if editor}
  <VehicleForm vehicle={editor.vehicle} onclose={() => (editor = null)} onsaved={saved} />
{/if}

<style>
  select {
    padding: 6px 9px;
    border: 1px solid var(--rule-strong);
    border-radius: var(--radius-sm);
    background: var(--sheet);
    color: var(--ink);
    font: inherit;
    font-size: 0.8125rem;
  }

  .id {
    display: flex;
    align-items: center;
    gap: 4px;
    margin-top: 2px;
    appearance: none;
    border: none;
    background: none;
    padding: 1px 0 0;
    font-family: var(--mono);
    font-size: 0.75rem;
    color: var(--ink-3);
    cursor: copy;
  }

  .id:hover {
    color: var(--accent);
  }
</style>
