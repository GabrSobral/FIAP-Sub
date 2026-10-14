<script>
  import { figure } from '../lib/format.js';

  let { amount, currency = 'BRL', large = false } = $props();

  let parts = $derived(figure(amount, currency));
</script>

<span class="figure" class:figure-lg={large}
  ><span class="symbol">{parts.symbol}</span><span class="value">{parts.value}</span></span
>
