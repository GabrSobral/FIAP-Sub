<script>
  import Modal from './Modal.svelte';
  import { sales, describe } from '../lib/api.js';
  import Figure from './Figure.svelte';
  import { maskCpf, isValidCpf, digits } from '../lib/format.js';
  import { toasts } from '../lib/toasts.svelte.js';
  import { saleLog } from '../lib/sale-log.svelte.js';

  let { vehicle, onclose, onbought } = $props();

  let cpf = $state('');
  let saleDate = $state('');
  let submitting = $state(false);
  let touched = $state(false);

  let cpfError = $derived.by(() => {
    if (!touched) return null;
    if (digits(cpf).length === 0) return 'Informe o CPF de quem está comprando.';
    if (digits(cpf).length < 11) return 'O CPF tem 11 dígitos.';
    if (!isValidCpf(cpf)) return 'Dígito verificador inválido — confira o número.';

    return null;
  });

  let canSubmit = $derived(isValidCpf(cpf) && !submitting);

  function onCpfInput(event) {
    cpf = maskCpf(event.currentTarget.value);
    touched = true;
  }

  async function submit(event) {
    event.preventDefault();
    touched = true;

    if (!canSubmit) return;

    submitting = true;

    try {
      const sale = await sales.openSale({
        vehicleId: vehicle.vehicleId,
        cpf,
        // datetime-local não tem fuso; manda como hora local e deixa o servidor interpretar.
        saleDate: saleDate ? new Date(saleDate).toISOString() : undefined,
      });

      saleLog.remember(sale, vehicle);
      toasts.success(`Compra aberta. Código de pagamento: ${sale.paymentCode}`);
      onbought?.(sale);
    } catch (error) {
      toasts.error(`Não foi possível comprar: ${describe(error)}`);
    } finally {
      submitting = false;
    }
  }
</script>

<Modal title="Comprar veículo" {onclose}>
  <form id="buy-form" class="stack" onsubmit={submit}>
    <div class="subject">
      <div class="grow">
        <strong>{vehicle.brand} {vehicle.model}</strong>
        <p class="small muted">{vehicle.year} · {vehicle.color}</p>
      </div>
      <Figure amount={vehicle.price} currency={vehicle.currency} large />
    </div>

    <div class="field" class:invalid={cpfError}>
      <label for="buy-cpf">CPF do comprador</label>
      <input
        id="buy-cpf"
        value={cpf}
        oninput={onCpfInput}
        inputmode="numeric"
        autocomplete="off"
        placeholder="000.000.000-00"
        maxlength="14"
        required
      />
      {#if cpfError}
        <span class="error">{cpfError}</span>
      {:else}
        <span class="hint">Conferido aqui por cortesia; quem decide é o domínio, no servidor.</span>
      {/if}
    </div>

    <div class="field">
      <label for="buy-date">Data da venda <span class="faint">(opcional)</span></label>
      <input id="buy-date" type="datetime-local" bind:value={saleDate} />
      <span class="hint">Em branco, vale o instante em que o pagamento for aprovado.</span>
    </div>

    <p class="small muted">
      A compra fica <em>aguardando pagamento</em> e devolve um código de pagamento. A venda só se
      efetiva quando o provedor confirmar esse código no webhook.
    </p>
  </form>

  {#snippet footer()}
    <button type="button" class="btn" onclick={onclose} disabled={submitting}>Cancelar</button>
    <button type="submit" form="buy-form" class="btn btn-primary" disabled={!canSubmit}>
      {#if submitting}<span class="spinner"></span>{/if}
      Confirmar compra
    </button>
  {/snippet}
</Modal>

<style>
  .subject {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 11px 13px;
    background: var(--sheet-2);
    border: 1px solid var(--rule);
    border-left: 2px solid var(--accent);
    border-radius: var(--radius-sm);
  }

  .subject strong {
    font-weight: 620;
  }
</style>
