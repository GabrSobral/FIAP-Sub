const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

export function at(iso) {
  return iso ? dateTime.format(new Date(iso)) : '—';
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

/**
 * Separa o símbolo dos dígitos. Numa listagem ordenada por preço o que precisa
 * alinhar é o algarismo, não o "R$" — com ele junto, cada linha começa em uma
 * posição diferente e a ordem crescente deixa de ser visível de relance.
 */
export function figure(amount, currency = 'BRL') {
  if (amount === null || amount === undefined) return { symbol: '', value: '—' };

  const parts = new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).formatToParts(
    amount,
  );
  const keep = (part) =>
    part.type !== 'currency' && !(part.type === 'literal' && part.value.trim() === '');

  return {
    symbol: parts
      .filter((part) => part.type === 'currency')
      .map((part) => part.value)
      .join(''),
    value: parts
      .filter(keep)
      .map((part) => part.value)
      .join(''),
  };
}

const relative = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' });

const STEPS = [
  ['second', 60],
  ['minute', 60],
  ['hour', 24],
  ['day', 7],
  ['week', 4.35],
  ['month', 12],
];

/** "agora mesmo", "há 2 minutos" — numa tela que se atualiza, isso lê melhor que um horário. */
export function ago(iso) {
  if (!iso) return '—';

  const elapsed = (Date.now() - new Date(iso).getTime()) / 1000;

  if (elapsed < 45) return 'agora mesmo';

  let amount = elapsed;

  for (const [unit, size] of STEPS) {
    if (Math.abs(amount) < size) return relative.format(-Math.round(amount), unit);

    amount /= size;
  }

  return relative.format(-Math.round(amount), 'year');
}
