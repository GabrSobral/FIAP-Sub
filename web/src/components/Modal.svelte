<script>
  import Icon from './Icon.svelte';

  let { title, onclose, children, footer } = $props();

  let dialog;

  // O <dialog> nativo já traz a armadilha de foco, o fundo e o Escape.
  $effect(() => {
    dialog?.showModal();
  });
</script>

<dialog bind:this={dialog} onclose={onclose} oncancel={onclose}>
  <header>
    <h3 class="grow">{title}</h3>
    <button type="button" class="btn btn-bare btn-icon" onclick={onclose} aria-label="Fechar">
      <Icon name="close" size={15} />
    </button>
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
    border: 1px solid var(--rule-strong);
    border-radius: var(--corner);
    background: var(--sheet);
    color: var(--ink);
    /* A folha levantada do balcão: só o que flutua ganha sombra. */
    box-shadow: var(--lift);
  }

  dialog::backdrop {
    background: rgb(16 18 20 / 52%);
  }

  header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 10px 12px 16px;
    border-bottom: 1px solid var(--rule);
    background: var(--sheet-2);
  }

  .body {
    padding: 16px;
  }

  footer {
    display: flex;
    justify-content: flex-end;
    gap: 9px;
    padding: 13px 16px;
    border-top: 1px solid var(--rule);
  }
</style>
