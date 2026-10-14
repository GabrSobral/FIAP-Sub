<script>
  const SEALS = {
    Available: { tone: 'seal-ok', label: 'à venda' },
    Reserved: { tone: 'seal-hold', label: 'reservado' },
    Sold: { tone: '', label: 'vendido' },
    PendingPayment: { tone: 'seal-hold', label: 'aguardando pagamento' },
    Paid: { tone: 'seal-ok', label: 'pago' },
    Cancelled: { tone: 'seal-stop', label: 'cancelado' },
  };

  let { status } = $props();

  let info = $derived(SEALS[status] ?? { tone: '', label: status ?? '—' });
</script>

<span class="seal {info.tone}" title={status}>{info.label}</span>
