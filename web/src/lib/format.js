const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
const dateOnly = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'long' });

/** The APIs always answer with an explicit currency, so honour it instead of assuming BRL. */
export function brl(amount, currency = 'BRL') {
  if (amount === null || amount === undefined) return '—';
  if (currency === 'BRL') return money.format(amount);

  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).format(amount);
}

export function at(iso) {
  return iso ? dateTime.format(new Date(iso)) : '—';
}

export function on(iso) {
  return iso ? dateOnly.format(new Date(iso)) : '—';
}

export const digits = (value) => (value ?? '').replace(/\D/g, '');

/** Formats as the CPF is typed: 000.000.000-00. */
export function maskCpf(value) {
  const only = digits(value).slice(0, 11);

  return only
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/^(\d{3})\.(\d{3})\.(\d{3})(\d)/, '$1.$2.$3-$4');
}

/**
 * Same check-digit algorithm the Cpf value object enforces on the server. Running it here is a
 * courtesy to the person typing, not a security boundary: the domain still has the final say.
 */
export function isValidCpf(value) {
  const only = digits(value);

  if (only.length !== 11) return false;
  if (/^(\d)\1{10}$/.test(only)) return false;

  const checkDigit = (upTo) => {
    let sum = 0;

    for (let index = 0; index < upTo; index += 1) {
      sum += Number(only[index]) * (upTo + 1 - index);
    }

    const remainder = (sum * 10) % 11;

    return String(remainder > 9 ? 0 : remainder);
  };

  return checkDigit(9) === only[9] && checkDigit(10) === only[10];
}

export const shortId = (id) => (id ? `${id.slice(0, 8)}…` : '—');
