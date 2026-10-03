<script>
  import { toasts } from '../lib/toasts.svelte.js';
</script>

<div class="toasts" role="status" aria-live="polite">
  {#each toasts.items as toast (toast.id)}
    <div class="toast {toast.kind}">
      <span class="grow">{toast.message}</span>
      <button type="button" class="btn btn-ghost btn-sm" onclick={() => toasts.dismiss(toast.id)}>
        ✕
      </button>
    </div>
  {/each}
</div>

<style>
  .toasts {
    position: fixed;
    right: 16px;
    bottom: 16px;
    z-index: 50;
    display: flex;
    flex-direction: column;
    gap: 8px;
    width: min(380px, calc(100vw - 32px));
  }

  .toast {
    display: flex;
    align-items: flex-start;
    gap: 8px;
    padding: 10px 8px 10px 14px;
    border: 1px solid var(--border);
    border-left-width: 3px;
    border-radius: var(--radius-sm);
    background: var(--surface);
    box-shadow: var(--shadow-lifted);
    font-size: 0.875rem;
    animation: slide-in 160ms ease-out;
  }

  .toast.success {
    border-left-color: var(--ok);
  }

  .toast.error {
    border-left-color: var(--danger);
  }

  .toast.info {
    border-left-color: var(--accent);
  }

  @keyframes slide-in {
    from {
      opacity: 0;
      transform: translateY(6px);
    }
  }
</style>
