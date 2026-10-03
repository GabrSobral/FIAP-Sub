<script>
  let { title, onclose, children, footer } = $props();

  let dialog;

  // Native <dialog> brings the focus trap, the backdrop and Escape for free.
  $effect(() => {
    dialog?.showModal();
  });
</script>

<dialog bind:this={dialog} onclose={onclose} oncancel={onclose}>
  <header>
    <h3>{title}</h3>
    <button type="button" class="btn btn-ghost" onclick={onclose} aria-label="Fechar">✕</button>
  </header>

  <div class="body">
    {@render children?.()}
  </div>

  {#if footer}
    <footer>{@render footer()}</footer>
  {/if}
</dialog>

<style>
  dialog {
    width: min(520px, calc(100vw - 32px));
    padding: 0;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    background: var(--surface);
    color: var(--text);
    box-shadow: var(--shadow-lifted);
  }

  dialog::backdrop {
    background: rgb(12 11 10 / 55%);
    backdrop-filter: blur(2px);
  }

  header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 10px 14px 18px;
    border-bottom: 1px solid var(--border);
  }

  .body {
    padding: 18px;
  }

  footer {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    padding: 14px 18px;
    border-top: 1px solid var(--border);
    background: var(--surface-2);
    border-radius: 0 0 var(--radius) var(--radius);
  }
</style>
