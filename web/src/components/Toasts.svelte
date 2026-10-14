<script>
  import Icon from './Icon.svelte';
  import { toasts } from '../lib/toasts.svelte.js';
</script>

<div class="toasts" role="status" aria-live="polite">
  {#each toasts.items as toast (toast.id)}
    <div class="toast {toast.kind}">
      <span class="grow">{toast.message}</span>
      <button
        type="button"
        class="btn btn-bare btn-icon btn-sm"
        onclick={() => toasts.dismiss(toast.id)}
        aria-label="Dispensar"
      >
        <Icon name="close" size={13} />
      </button>
    </div>
  {/each}
</div>

<style>
  .toasts {
    position: fixed;
    right: 18px;
    bottom: 18px;
    z-index: 50;
    display: flex;
    flex-direction: column;
    gap: 8px;
    width: min(390px, calc(100vw - 36px));
  }

  .toast {
    display: flex;
    align-items: flex-start;
    gap: 8px;
    padding: 9px 6px 9px 13px;
    border: 1px solid var(--rule-strong);
    border-left-width: 3px;
    border-radius: var(--radius);
    background: var(--sheet);
    box-shadow: var(--lift);
    font-size: 0.875rem;
    animation: nudge 150ms ease-out;
  }

  .toast.success {
    border-left-color: var(--ok);
  }

  .toast.error {
    border-left-color: var(--stop);
  }

  .toast.info {
    border-left-color: var(--accent);
  }

  @keyframes nudge {
    from {
      opacity: 0;
      transform: translateY(5px);
    }
  }
</style>
