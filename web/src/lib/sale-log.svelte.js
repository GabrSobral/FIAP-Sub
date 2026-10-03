// The sales API exposes a sale only by its id (GET /api/sales/{id}) — there is no "list sales"
// endpoint, and adding one was out of scope for the brief. So this console remembers the
// purchases it opened, in this browser, and re-reads each one from the API to show live state.
const KEY = 'fiapsub.sales.v1';

function read() {
  try {
    const stored = JSON.parse(localStorage.getItem(KEY) ?? '[]');

    return Array.isArray(stored) ? stored : [];
  } catch {
    return [];
  }
}

let entries = $state(read());

function persist() {
  try {
    localStorage.setItem(KEY, JSON.stringify(entries));
  } catch {
    // A full or blocked storage is not worth failing a purchase over.
  }
}

export const saleLog = {
  get entries() {
    return entries;
  },

  remember(sale, vehicle) {
    if (entries.some((entry) => entry.id === sale.id)) return;

    entries.unshift({
      id: sale.id,
      vehicleId: sale.vehicleId,
      paymentCode: sale.paymentCode,
      buyerCpf: sale.buyerCpf,
      amount: sale.amount,
      currency: sale.currency,
      status: sale.status,
      saleDate: sale.saleDate,
      label: vehicle ? `${vehicle.brand} ${vehicle.model}` : null,
      openedAt: sale.createdAt,
    });

    persist();
  },

  update(sale) {
    const entry = entries.find((item) => item.id === sale.id);

    if (!entry) return;

    entry.status = sale.status;
    entry.saleDate = sale.saleDate;
    persist();
  },

  forget(id) {
    const index = entries.findIndex((entry) => entry.id === id);

    if (index !== -1) entries.splice(index, 1);

    persist();
  },

  clear() {
    entries.length = 0;
    persist();
  },
};
