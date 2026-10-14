// A API de vendas expõe uma venda só por id (GET /api/sales/{id}): não existe listagem,
// e criar uma estava fora do escopo do enunciado. Então esta tela guarda as compras que ela
// mesma abriu, neste navegador, e relê cada uma da API para mostrar o estado atual.
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
    // Storage cheio ou bloqueado não é motivo para derrubar uma compra.
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
