<script>
  import StatusChip from './StatusChip.svelte';
  import Figure from './Figure.svelte';
  import Icon from './Icon.svelte';
  import { core, sales, describe } from '../lib/api.js';
  import { at } from '../lib/format.js';
  import { toasts } from '../lib/toasts.svelte.js';
  import { saleLog } from '../lib/sale-log.svelte.js';

  let busy = $state(null);
  let manualCode = $state('');
  let lookupId = $state('');
  let lookedUp = $state(null);
  let lookingUp = $state(false);

  /** Chama o webhook de verdade no core, no lugar do provedor de pagamento. */
  async function notify(paymentCode, status, saleId) {
    busy = paymentCode;

    try {
      const result = await core.notifyPayment({ paymentCode, status });

      if (result.alreadyProcessed) {
        toasts.info('Esse código já havia sido notificado — nada mudou.');
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

  let pending = $derived(saleLog.entries.filter((entry) => entry.status === 'PendingPayment').length);
</script>

<section class="stack">
  <header class="page">
    <div class="grow">
      <h2>Pagamentos</h2>
      <p class="lede">
        O webhook mora no core: a partir do código do pagamento ele informa se o pagamento foi
        efetuado ou cancelado. Esta tela faz o papel do provedor.
      </p>
    </div>

    <div class="row">
      <button
        type="button"
        class="btn btn-sm"
        onclick={refreshAll}
        disabled={busy !== null || saleLog.entries.length === 0}
      >
        {#if busy === 'all'}<span class="spinner"></span>{:else}<Icon name="refresh" size={14} />{/if}
        Reler vendas
      </button>
      {#if saleLog.entries.length > 0}
        <button type="button" class="btn btn-bare btn-sm" onclick={() => saleLog.clear()}>
          Limpar
        </button>
      {/if}
    </div>
  </header>

  <div class="sheet">
    <div class="sheet-head">
      <h3>Compras abertas neste navegador</h3>
      {#if pending > 0}
        <span class="seal seal-hold">{pending} aguardando pagamento</span>
      {/if}
    </div>

    <div class="sheet-body flush">
      {#if saleLog.entries.length === 0}
        <div class="blank">
          <span class="blank-mark"><Icon name="receipt" /></span>
          <div>
            <strong>Nenhuma compra aberta ainda.</strong>
            <p>
              Compre um veículo na Vitrine e ele aparece aqui com o código de pagamento. A API
              expõe uma venda só por id, então esta lista é a memória deste navegador — não um
              endpoint.
            </p>
          </div>
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
                    <span class="sub mono">{entry.id.slice(0, 8)}</span>
                  </td>
                  <td class="mono">{entry.buyerCpf}</td>
                  <td class="num"><Figure amount={entry.amount} currency={entry.currency} /></td>
                  <td class="mono code">{entry.paymentCode}</td>
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
                          class="btn btn-stop btn-sm"
                          disabled={busy !== null}
                          onclick={() => notify(entry.paymentCode, 'cancelled', entry.id)}
                        >
                          Cancelar
                        </button>
                      </div>
                    {:else}
                      <button
                        type="button"
                        class="btn btn-bare btn-sm"
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
    <div class="sheet">
      <div class="sheet-head"><h3>Notificar um código à mão</h3></div>
      <div class="sheet-body stack">
        <div class="field">
          <label for="manual-code">Código do pagamento</label>
          <input id="manual-code" class="mono" bind:value={manualCode} placeholder="PAY-…" />
          <span class="hint">
            Reenviar o mesmo código é inofensivo: o webhook é idempotente e responde
            <code>alreadyProcessed</code>. Um código que não existe volta 404.
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
            class="btn btn-stop btn-sm"
            disabled={busy !== null || !manualCode.trim()}
            onclick={() => sendManual('cancelled')}
          >
            Cancelar
          </button>
        </div>
      </div>
    </div>

    <div class="sheet">
      <div class="sheet-head"><h3>Consultar uma venda</h3></div>
      <div class="sheet-body stack">
        <form class="stack" onsubmit={lookup}>
          <div class="field">
            <label for="lookup-id">Id da venda</label>
            <input id="lookup-id" class="mono" bind:value={lookupId} placeholder="01a1…" />
          </div>
          <div class="row">
            <button type="submit" class="btn btn-sm" disabled={lookingUp || !lookupId.trim()}>
              {#if lookingUp}<span class="spinner"></span>{:else}<Icon name="search" size={14} />{/if}
              Consultar
            </button>
          </div>
        </form>

        {#if lookedUp}
          <dl class="ficha">
            <dt>Status</dt>
            <dd><StatusChip status={lookedUp.status} /></dd>

            <dt>Comprador</dt>
            <dd class="mono">{lookedUp.buyerCpf}</dd>

            <dt>Valor</dt>
            <dd><Figure amount={lookedUp.amount} currency={lookedUp.currency} /></dd>

            <dt>Código</dt>
            <dd class="mono code">{lookedUp.paymentCode}</dd>

            <dt>Data da venda</dt>
            <dd>{at(lookedUp.saleDate)}</dd>

            {#if lookedUp.cancellationReason}
              <dt>Motivo</dt>
              <dd class="muted">{lookedUp.cancellationReason}</dd>
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
    grid-template-columns: repeat(auto-fit, minmax(310px, 1fr));
    gap: 18px;
    align-items: start;
  }

  .sub {
    display: block;
    font-size: 0.75rem;
    color: var(--ink-3);
  }

  .code {
    font-size: 0.75rem;
    letter-spacing: -0.01em;
  }

  /* Ficha de duas colunas: rótulo à esquerda, valor à direita, uma régua por linha. */
  .ficha {
    display: grid;
    grid-template-columns: 8.5rem minmax(0, 1fr);
    margin: 0;
    border-top: 1px solid var(--rule);
    font-size: 0.875rem;
  }

  .ficha dt {
    padding: 7px 10px 7px 0;
    border-bottom: 1px solid var(--rule);
    color: var(--ink-2);
    font-size: 0.8125rem;
  }

  .ficha dd {
    margin: 0;
    padding: 7px 0;
    border-bottom: 1px solid var(--rule);
    min-width: 0;
    overflow-wrap: anywhere;
  }

  .ficha dt:last-of-type,
  .ficha dd:last-of-type {
    border-bottom: none;
  }
</style>
