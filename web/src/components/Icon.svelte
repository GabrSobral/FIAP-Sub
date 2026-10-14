<script>
  // Um conjunto pequeno e próprio: 16px na grade, traço de 1.6, sem preenchimento,
  // sempre em currentColor. Emoji sairia diferente em cada sistema operacional.
  const PATHS = {
    tag: 'M2.6 8.1 8.1 2.6a1.4 1.4 0 0 1 1-.4h3.5a.8.8 0 0 1 .8.8v3.5a1.4 1.4 0 0 1-.4 1l-5.5 5.5a1.4 1.4 0 0 1-2 0L2.6 10.1a1.4 1.4 0 0 1 0-2ZM10.6 5.4h.01',
    car: 'M2.4 9.6h11.2M4 9.6l1.3-3.7a1.5 1.5 0 0 1 1.4-1h2.6a1.5 1.5 0 0 1 1.4 1l1.3 3.7M2.4 9.6v2.2h11.2V9.6M6 11.8a1.1 1.1 0 1 1-2.2 0 1.1 1.1 0 0 1 2.2 0M12.4 11.8a1.1 1.1 0 1 1-2.2 0 1.1 1.1 0 0 1 2.2 0',
    sheet: 'M5.8 2.6h4.4v2H5.8zM4.6 3.6h-1a.8.8 0 0 0-.8.8v8.2a.8.8 0 0 0 .8.8h8.8a.8.8 0 0 0 .8-.8V4.4a.8.8 0 0 0-.8-.8h-1M5.6 7.8h4.8M5.6 10.2h2.8',
    receipt: 'M3.6 2.2h8.8v11.6l-2.2-1.2-2.2 1.2-2.2-1.2-2.2 1.2ZM5.8 5.2h4.4M5.8 7.6h4.4M5.8 10h2.4',
    refresh: 'M13.4 8a5.4 5.4 0 0 1-9.3 3.7M2.6 8a5.4 5.4 0 0 1 9.3-3.7M11.9 1.9v2.5h-2.5M4.1 14.1v-2.5h2.5',
    plus: 'M8 3.4v9.2M3.4 8h9.2',
    close: 'M4 4l8 8M12 4l-8 8',
    copy: 'M5.6 5.6V3.4h7v7h-2.2M3.4 5.6h7v7h-7z',
    search: 'M7.2 11.6a4.4 4.4 0 1 0 0-8.8 4.4 4.4 0 0 0 0 8.8ZM10.5 10.5l2.9 2.9',
    inbox: 'M2.2 8.6h3.1l1 1.7h3.4l1-1.7h3.1M2.2 8.6 4 3.4h8l1.8 5.2v4H2.2z',
    check: 'M3.4 8.4l3.1 3.1 6.1-6.8',
    arrow: 'M8 3.2v9.6M4.4 9.2 8 12.8l3.6-3.6',
    alert: 'M8 5.6v3.6M8 11.6h.01M8 2.2 1.8 13.2h12.4z',
  };

  let { name, size = 16, label = null } = $props();

  let d = $derived(PATHS[name] ?? PATHS.tag);
</script>

<svg
  width={size}
  height={size}
  viewBox="0 0 16 16"
  fill="none"
  stroke="currentColor"
  stroke-width="1.6"
  stroke-linecap="round"
  stroke-linejoin="round"
  role={label ? 'img' : 'presentation'}
  aria-label={label}
  aria-hidden={label ? undefined : 'true'}
  focusable="false"
>
  <path {d} />
</svg>

<style>
  svg {
    flex: none;
    display: block;
  }
</style>
