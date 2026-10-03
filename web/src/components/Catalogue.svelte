<script>
  import StatusChip from './StatusChip.svelte';
  import VehicleForm from './VehicleForm.svelte';
  import { core, describe } from '../lib/api.js';
  import { brl, at } from '../lib/format.js';

  let page = $state({ items: [], totalCount: 0 });
  let status = $state('');
  let loading = $state(true);
  let failure = $state(null);
  let editor = $state(null); // { vehicle: null } = register, { vehicle } = edit

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
    } catch {
      // Clipboard access can be denied; the id is visible in the title attribute anyway.
    }
  }
</script>

<section class="stack">
  <div class="panel">
    <div class="panel-head">
      <div>
        <h2>Catálogo</h2>
        <p class="small muted">
          Mantido pelo serviço core, que é a única fonte de verdade do estado de um veículo. Cada
          mudança aqui é publicada por HTTP para o serviço de vendas.
        </p>
      </div>

      <div class="row">
        <div class="field inline">
          <label class="sr-only" for="filter-status">Filtrar por status</label>
          <select id="filter-status" bind:value={status}>
            <option value="">Todos os status</option>
            <option value="Available">À venda</option>
            <option value="Reserved">Reservados</option>
            <option value="Sold">Vendidos</option>
          </select>
        </div>

        <button type="button" class="btn btn-sm" onclick={load} disabled={loading}>
          {#if loading}<span class="spinner"></span>{/if}
          Atualizar
        </button>

        <button type="button" class="btn btn-primary btn-sm" onclick={() => (editor = { vehicle: null })}>
          Cadastrar veículo
        </button>
      </div>
    </div>

    <div class="panel-body tight">
      {#if failure}
        <div class="empty">
          <strong>O serviço core não respondeu</strong>
          <p class="small">{failure}</p>
        </div>
      {:else if loading && page.items.length === 0}
        <div class="empty"><span class="spinner"></span></div>
      {:else if page.items.length === 0}
        <div class="empty">
          <strong>Catálogo vazio</strong>
          <p class="small">Cadastre o primeiro veículo para que ele apareça na vitrine.</p>
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
                      class="id mono faint"
                      title="Copiar {vehicle.id}"
                      onclick={() => copy(vehicle.id)}
                    >
                      {vehicle.id.slice(0, 8)}
                    </button>
                  </td>
                  <td class="tnum">{vehicle.year}</td>
                  <td>{vehicle.color}</td>
                  <td class="num">{brl(vehicle.price, vehicle.currency)}</td>
                  <td><StatusChip status={vehicle.status} /></td>
                  <td class="mono faint">{vehicle.paymentCode ?? '—'}</td>
                  <td class="num">{vehicle.version}</td>
                  <td class="small muted">{at(vehicle.updatedAt)}</td>
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

        <footer class="count small muted">
          {page.totalCount} {page.totalCount === 1 ? 'veículo' : 'veículos'} no catálogo
        </footer>
      {/if}
    </div>
  </div>
</section>

{#if editor}
  <VehicleForm vehicle={editor.vehicle} onclose={() => (editor = null)} onsaved={saved} />
{/if}

<style>
  .field.inline {
    min-width: 160px;
  }

  .id {
    display: block;
    appearance: none;
    border: none;
    background: none;
    padding: 0;
    font-family: var(--mono);
    font-size: 0.78rem;
    color: var(--text-faint);
    cursor: copy;
  }

  .id:hover {
    color: var(--accent);
  }

  .count {
    padding: 11px 18px;
    border-top: 1px solid var(--border);
    background: var(--surface-2);
    border-radius: 0 0 var(--radius) var(--radius);
  }
</style>
