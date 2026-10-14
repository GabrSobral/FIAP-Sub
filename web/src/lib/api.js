// As duas APIs são alcançadas por prefixos do mesmo origin, reescritos por um proxy:
// o dev server do Vite no `npm run dev`, o nginx dentro do container. Nada aqui é
// cross-origin, então não há preflight e nenhum dos serviços precisa de CORS.
const CORE = import.meta.env.VITE_CORE_API ?? '/core-api';
const SALES = import.meta.env.VITE_SALES_API ?? '/sales-api';

/** Resposta fora do 2xx, carregando o ProblemDetails que os serviços devolvem. */
export class ApiError extends Error {
  constructor(status, problem, fallbackMessage) {
    super(problem?.detail || problem?.title || fallbackMessage);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  /** Código estável, do tipo `vehicle.not_available`. */
  get code() {
    return this.problem?.code ?? null;
  }

  /** Mensagens por campo, presentes quando a falha veio da validação. */
  get fieldErrors() {
    return this.problem?.errors ?? null;
  }
}

/** A rede não respondeu. Problema diferente de "o serviço disse não". */
export class NetworkError extends Error {
  constructor(cause) {
    super('Não foi possível falar com o serviço.');
    this.name = 'NetworkError';
    this.cause = cause;
  }
}

function parse(text) {
  if (!text) return null;

  try {
    return JSON.parse(text);
  } catch {
    return { detail: text };
  }
}

async function request(base, path, { method = 'GET', body, signal } = {}) {
  const hasBody = body !== undefined;
  let response;

  try {
    response = await fetch(base + path, {
      method,
      headers: hasBody ? { 'Content-Type': 'application/json' } : undefined,
      body: hasBody ? JSON.stringify(body) : undefined,
      signal,
    });
  } catch (cause) {
    if (cause.name === 'AbortError') throw cause;
    throw new NetworkError(cause);
  }

  if (response.status === 204) return null;

  const payload = parse(await response.text());

  if (!response.ok) {
    throw new ApiError(response.status, payload, `HTTP ${response.status}`);
  }

  return payload;
}

const query = (params) => {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, value);
  }

  const text = search.toString();

  return text ? `?${text}` : '';
};

/** Catálogo de veículos e webhook de pagamento. */
export const core = {
  ready: (signal) => request(CORE, '/health/ready', { signal }),

  registerVehicle: (vehicle, signal) =>
    request(CORE, '/api/vehicles', { method: 'POST', body: vehicle, signal }),

  updateVehicle: (id, vehicle, signal) =>
    request(CORE, `/api/vehicles/${id}`, { method: 'PUT', body: vehicle, signal }),

  getVehicle: (id, signal) => request(CORE, `/api/vehicles/${id}`, { signal }),

  listVehicles: ({ status, page = 1, pageSize = 50 } = {}, signal) =>
    request(CORE, `/api/vehicles${query({ status, page, pageSize })}`, { signal }),

  /**
   * Faz o papel do provedor. É o webhook de verdade, chamado com o código que o
   * serviço de vendas entregou quando a compra foi aberta.
   */
  notifyPayment: ({ paymentCode, status }, signal) =>
    request(CORE, '/api/webhooks/payments', { method: 'POST', body: { paymentCode, status }, signal }),
};

/** Listagens públicas e compras. */
export const sales = {
  ready: (signal) => request(SALES, '/health/ready', { signal }),

  listForSale: ({ page = 1, pageSize = 50 } = {}, signal) =>
    request(SALES, `/api/vehicles/for-sale${query({ page, pageSize })}`, { signal }),

  listSold: ({ page = 1, pageSize = 50 } = {}, signal) =>
    request(SALES, `/api/vehicles/sold${query({ page, pageSize })}`, { signal }),

  openSale: ({ vehicleId, cpf, saleDate }, signal) =>
    request(SALES, '/api/sales', { method: 'POST', body: { vehicleId, cpf, saleDate }, signal }),

  getSale: (id, signal) => request(SALES, `/api/sales/${id}`, { signal }),
};

/** Transforma qualquer erro numa frase que vale mostrar para alguém. */
export function describe(error) {
  if (error instanceof ApiError) {
    const fields = error.fieldErrors;

    if (fields) {
      return Object.entries(fields)
        .map(([field, messages]) => `${field}: ${messages.join(' ')}`)
        .join(' · ');
    }

    return error.message;
  }

  if (error instanceof NetworkError) return error.message;

  return error?.message ?? 'Erro inesperado.';
}
