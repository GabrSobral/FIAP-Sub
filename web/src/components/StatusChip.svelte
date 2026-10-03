<script>
  // Vehicle and sale statuses share the same vocabulary of meaning: waiting, done, undone.
  const TONES = {
    Available: { tone: 'chip-ok', label: 'À venda' },
    Reserved: { tone: 'chip-warn', label: 'Reservado' },
    Sold: { tone: 'chip-neutral', label: 'Vendido' },
    PendingPayment: { tone: 'chip-warn', label: 'Aguardando pagamento' },
    Paid: { tone: 'chip-ok', label: 'Pago' },
    Cancelled: { tone: 'chip-danger', label: 'Cancelado' },
  };

  let { status } = $props();

  let info = $derived(TONES[status] ?? { tone: 'chip-neutral', label: status ?? '—' });
</script>

<span class="chip {info.tone}" title={status}>{info.label}</span>
