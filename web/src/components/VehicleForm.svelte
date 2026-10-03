<script>
  import { untrack } from 'svelte';
  import Modal from './Modal.svelte';
  import { core, describe } from '../lib/api.js';
  import { toasts } from '../lib/toasts.svelte.js';

  // `vehicle` absent means "register"; present means "edit that one".
  let { vehicle = null, onclose, onsaved } = $props();

  const nextYear = new Date().getFullYear() + 1;

  // Read once, on purpose: this is a form seeded from the vehicle, not a live mirror of it.
  // A new vehicle means a new modal instance, so there is nothing to keep in sync.
  const initial = untrack(() => vehicle) ?? {};

  let brand = $state(initial.brand ?? '');
  let model = $state(initial.model ?? '');
  let year = $state(initial.year ?? nextYear - 1);
  let color = $state(initial.color ?? '');
  let price = $state(initial.price ?? '');
  let submitting = $state(false);
  let fieldErrors = $state(null);

  let editing = $derived(vehicle !== null);

  let canSubmit = $derived(
    brand.trim() !== '' &&
      model.trim() !== '' &&
      color.trim() !== '' &&
      Number(year) >= 1900 &&
      Number(year) <= nextYear &&
      Number(price) > 0 &&
      !submitting,
  );

  async function submit(event) {
    event.preventDefault();

    if (!canSubmit) return;

    submitting = true;
    fieldErrors = null;

    const payload = {
      brand: brand.trim(),
      model: model.trim(),
      year: Number(year),
      color: color.trim(),
      price: Number(price),
    };

    try {
      const saved = editing
        ? await core.updateVehicle(vehicle.id, payload)
        : await core.registerVehicle(payload);

      toasts.success(
        editing
          ? `${saved.brand} ${saved.model} atualizado — versão ${saved.version}.`
          : `${saved.brand} ${saved.model} cadastrado e publicado na vitrine.`,
      );
      onsaved?.(saved);
    } catch (error) {
      fieldErrors = error.fieldErrors ?? null;
      toasts.error(`${editing ? 'Não foi possível atualizar' : 'Não foi possível cadastrar'}: ${describe(error)}`);
    } finally {
      submitting = false;
    }
  }

  const errorFor = (name) => fieldErrors?.[name]?.join(' ') ?? null;
</script>

<Modal title={editing ? 'Editar veículo' : 'Cadastrar veículo para venda'} {onclose}>
  <form id="vehicle-form" class="stack" onsubmit={submit}>
    <div class="grid-2">
      <div class="field" class:invalid={errorFor('Brand')}>
        <label for="v-brand">Marca</label>
        <input id="v-brand" bind:value={brand} maxlength="60" required />
        {#if errorFor('Brand')}<span class="error">{errorFor('Brand')}</span>{/if}
      </div>

      <div class="field" class:invalid={errorFor('Model')}>
        <label for="v-model">Modelo</label>
        <input id="v-model" bind:value={model} maxlength="80" required />
        {#if errorFor('Model')}<span class="error">{errorFor('Model')}</span>{/if}
      </div>

      <div class="field" class:invalid={errorFor('Year')}>
        <label for="v-year">Ano</label>
        <input id="v-year" type="number" bind:value={year} min="1900" max={nextYear} required />
        <span class="hint">De 1900 até {nextYear}.</span>
        {#if errorFor('Year')}<span class="error">{errorFor('Year')}</span>{/if}
      </div>

      <div class="field" class:invalid={errorFor('Color')}>
        <label for="v-color">Cor</label>
        <input id="v-color" bind:value={color} maxlength="30" required />
        {#if errorFor('Color')}<span class="error">{errorFor('Color')}</span>{/if}
      </div>
    </div>

    <div class="field" class:invalid={errorFor('Price')}>
      <label for="v-price">Preço (BRL)</label>
      <input id="v-price" type="number" bind:value={price} min="0.01" step="0.01" required />
      <span class="hint">Maior que zero, no máximo duas casas decimais.</span>
      {#if errorFor('Price')}<span class="error">{errorFor('Price')}</span>{/if}
    </div>

    {#if editing}
      <p class="small muted">
        Só um veículo <em>à venda</em> pode ser editado. Com uma compra em andamento ou já vendido,
        o serviço responde 409 — mudar o preço no meio da reserva desalinharia o valor da venda.
      </p>
    {/if}
  </form>

  {#snippet footer()}
    <button type="button" class="btn" onclick={onclose} disabled={submitting}>Cancelar</button>
    <button type="submit" form="vehicle-form" class="btn btn-primary" disabled={!canSubmit}>
      {#if submitting}<span class="spinner"></span>{/if}
      {editing ? 'Salvar' : 'Cadastrar'}
    </button>
  {/snippet}
</Modal>
