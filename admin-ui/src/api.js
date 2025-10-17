export const API_BASE =
  import.meta.env.VITE_API_BASE?.replace(/\/+$/, "") || "http://localhost:8080";

export async function listCards() {
  const r = await fetch(`${API_BASE}/cards`);
  if (!r.ok) throw new Error(`listCards failed: ${r.status}`);
  return r.json();
}

export async function createCard(card) {
  const r = await fetch(`${API_BASE}/cards`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(card),
  });
  const data = await r.json().catch(() => ({}));
  if (!r.ok) {
    const msg = data?.errors?.join(", ") || `create failed: ${r.status}`;
    throw new Error(msg);
  }
  return data;
}

export async function deleteCard(id) {
  const r = await fetch(`${API_BASE}/cards/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
  if (!r.ok) throw new Error(`delete failed: ${r.status}`);
  return r.json();
}
