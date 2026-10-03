<script>
  import StatusChip from './StatusChip.svelte';
  import { core, sales, describe } from '../lib/api.js';
  import { brl, at } from '../lib/format.js';
  import { toasts } from '../lib/toasts.svelte.js';
  import { saleLog } from '../lib/sale-log.svelte.js';

  let busy = $state(null);
  let manualCode = $state('');
  let lookupId = $state('');
  let lookedUp = $state(null);
  let lookingUp = $state(false);

  /** Calls the real webhook on the core service, standing in for the payment provider. */
  async function notify(paymentCode, status, saleId) {
    busy = paymentCode;

    try {
      const result = await core.notifyPayment({ paymentCode, status });

      if (result.alreadyProcessed) {
        toasts.info(`Esse código já havia sido notificado — nada mudou (idempotente).`);
      } else {
        toasts.success(
          status === 'approved'
            ? `Pagamento aprovado. Veículo em ${result.vehicleStatus}.`
            : `Pagamento cancelado. Veículo em ${result.vehicleStatus}.`,
        );
      }

      if (saleId) await refresh(saleId);
    } catch (error) {
      toasts.error(`Webhook recusado: ${describe(error)}`);
    } finally {
      busy = null;
    }
  }

  async function refresh(saleId) {
    try {
      saleLog.update(await sales.getSale(saleId));
    } catch (error) {
      toasts.error(`Não foi possível reler a venda: ${describe(error)}`);
    }
  }

  async function refreshAll() {
    busy = 'all';

    await Promise.all(saleLog.entries.map((entry) => refresh(entry.id)));

    busy = null;
  }

  async function lookup(event) {
    event.preventDefault();

    if (!lookupId.trim()) return;

    lookingUp = true;
    lookedUp = null;

    try {
      lookedUp = await sales.getSale(lookupId.trim());
    } catch (error) {
      toasts.error(`Venda não encontrada: ${describe(error)}`);
    } finally {
      lookingUp = false;
    }
  }

  function sendManual(status) {
    if (!manualCode.trim()) return;

    notify(manualCode.trim(), status, null);
  }
</script>

<section class="stack">
  <div class="panel">
    <div class="panel-head">
      <div>
        <h2>Pagamentos</h2>
        <p class="small muted">
          O webhook fica no serviço core: a partir do código do pagamento ele informa se o
          pagamento foi efetuado ou cancelado. Esta tela faz o papel do provedor.
        </p>
      </div>

      <div class="row">
        <button
          type="button"
          class="btn btn-sm"
          onclick={refreshAll}
          disabled={busy !== null || saleLog.entries.length === 0}
        >
          {#if busy === 'all'}<span class="spinner"></span>{/if}
          Reler vendas
        </button>
        {#if saleLog.entries.length > 0}
          <button type="button" class="btn btn-ghost btn-sm" onclick={() => saleLog.clear()}>
            Limpar lista
          </button>
        {/if}
      </div>
    </div>

    <div class="panel-body tight">
      {#if saleLog.entries.length === 0}
        <div class="empty">
          <strong>Nenhuma compra aberta por este navegador</strong>
          <p class="small">
            Compre um veículo na Vitrine e ele aparece aqui com o código de pagamento. A API expõe
            uma venda apenas por id, então esta lista é a memória local deste navegador.
          </p>
        </div>
      {:else}
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Veículo</th>
                <th>CPF</th>
                <th class="num">Valor</th>
                <th>Código do pagamento</th>
                <th>Status</th>
                <th>Data da venda</th>
                <th><span class="sr-only">Ações</span></th>
              </tr>
            </thead>
            <tbody>
              {#each saleLog.entries as entry (entry.id)}
                <tr>
                  <td>
                    <strong>{entry.label ?? 'Veículo'}</strong>
                    <span class="small faint mono">{entry.id.slice(0, 8)}</span>
                  </td>
                  <td class="mono">{entry.buyerCpf}</td>
                  <td class="num">{brl(entry.amount, entry.currency)}</td>
                  <td class="mono small">{entry.paymentCode}</td>
                  <td><StatusChip status={entry.status} /></td>
                  <td class="small muted">{at(entry.saleDate)}</td>
                  <td>
                    {#if entry.status === 'PendingPayment'}
                      <div class="row" style="gap: 6px">
                        <button
                          type="button"
                          class="btn btn-ok btn-sm"
                          disabled={busy !== null}
                          onclick={() => notify(entry.paymentCode, 'approved', entry.id)}
                        >
                          {#if busy === entry.paymentCode}<span class="spinner"></span>{/if}
                          Aprovar
                        </button>
                        <button
                          type="button"
                          class="btn btn-danger btn-sm"
                          disabled={busy !== null}
                          onclick={() => notify(entry.paymentCode, 'cancelled', entry.id)}
                        >
                          Cancelar
                        </button>
                      </div>
                    {:else}
                      <button
                        type="button"
                        class="btn btn-ghost btn-sm"
                        onclick={() => saleLog.forget(entry.id)}
                      >
                        Remover
                      </button>
                    {/if}
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    </div>
  </div>

  <div class="two-up">
    <div class="panel">
      <div class="panel-head"><h3>Notificar um código à mão</h3></div>
      <div class="panel-body stack">
        <div class="field">
          <label for="manual-code">Código do pagamento</label>
          <input id="manual-code" class="mono" bind:value={manualCode} placeholder="PAY-…" />
          <span class="hint">
            Reenviar o mesmo código é inofensivo: o webhook é idempotente e responde
            <code>alreadyProcessed</code>.
          </span>
        </div>

        <div class="row">
          <button
            type="button"
            class="btn btn-ok btn-sm"
            disabled={busy !== null || !manualCode.trim()}
            onclick={() => sendManual('approved')}
          >
            Aprovar
          </button>
          <button
            type="button"
            class="btn btn-danger btn-sm"
            disabled={busy !== null || !manualCode.trim()}
            onclick={() => sendManual('cancelled')}
          >
            Cancelar
          </button>
        </div>
      </div>
    </div>

    <div class="panel">
      <div class="panel-head"><h3>Consultar uma venda</h3></div>
      <div class="panel-body stack">
        <form class="stack" onsubmit={lookup}>
          <div class="field">
            <label for="lookup-id">Id da venda</label>
            <input id="lookup-id" class="mono" bind:value={lookupId} placeholder="01a1…" />
          </div>
          <div class="row">
            <button type="submit" class="btn btn-sm" disabled={lookingUp || !lookupId.trim()}>
              {#if lookingUp}<span class="spinner"></span>{/if}
              Consultar
            </button>
          </div>
        </form>

        {#if lookedUp}
          <dl class="detail">
            <dt>Status</dt>
            <dd><StatusChip status={lookedUp.status} /></dd>

            <dt>Comprador</dt>
            <dd class="mono">{lookedUp.buyerCpf}</dd>

            <dt>Valor</dt>
            <dd class="tnum">{brl(lookedUp.amount, lookedUp.currency)}</dd>

            <dt>Código</dt>
            <dd class="mono small">{lookedUp.paymentCode}</dd>

            <dt>Data da venda</dt>
            <dd>{at(lookedUp.saleDate)}</dd>

            {#if lookedUp.cancellationReason}
              <dt>Motivo</dt>
              <dd class="small muted">{lookedUp.cancellationReason}</dd>
            {/if}
          </dl>
        {/if}
      </div>
    </div>
  </div>
</section>

<style>
  .two-up {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 14px;
  }

  .detail {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 6px 14px;
    margin: 0;
    padding-top: 12px;
    border-top: 1px solid var(--border);
    font-size: 0.875rem;
  }

  .detail dt {
    color: var(--text-faint);
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    padding-top: 2px;
  }

  .detail dd {
    margin: 0;
  }
</style>
