let items = $state([]);
let nextId = 0;

function push(kind, message, timeout) {
  const id = (nextId += 1);

  items.push({ id, kind, message });

  if (timeout) setTimeout(() => dismiss(id), timeout);

  return id;
}

function dismiss(id) {
  const index = items.findIndex((item) => item.id === id);

  if (index !== -1) items.splice(index, 1);
}

export const toasts = {
  get items() {
    return items;
  },
  success: (message) => push('success', message, 5000),
  info: (message) => push('info', message, 5000),
  // Failures stay until dismissed: they usually need reading, and sometimes copying.
  error: (message) => push('error', message),
  dismiss,
};
