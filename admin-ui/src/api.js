const API = import.meta.env.VITE_API_BASE || "http://localhost:8080";

function authHeader() {
  const t = localStorage.getItem("token");
  return t ? { Authorization: `Bearer ${t}` } : {};
}

export async function login(username, password) {
  const r = await fetch(`${API}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password })
  });
  if (!r.ok) throw new Error("Login failed");
  return r.json();
}

export async function listCards() {
  const r = await fetch(`${API}/api/cards`, { headers: { ...authHeader() } });
  if (!r.ok) throw new Error("Fetch cards failed");
  return r.json();
}

export async function createCard(card) {
  const r = await fetch(`${API}/api/cards`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeader() },
    body: JSON.stringify(card)
  });
  if (!r.ok) throw new Error("Create failed");
  return r.json();
}

export async function updateCard(id, card) {
  const r = await fetch(`${API}/api/cards/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeader() },
    body: JSON.stringify(card)
  });
  if (!r.ok) throw new Error("Update failed");
  return r.json();
}

export async function deleteCard(id) {
  const r = await fetch(`${API}/api/cards/${id}`, {
    method: "DELETE",
    headers: { ...authHeader() }
  });
  if (!r.ok) throw new Error("Delete failed");
  return r.json();
}

export async function uploadImage(file) {
  const fd = new FormData();
  fd.append("file", file);
  const r = await fetch(`${API}/api/upload`, {
    method: "POST",
    headers: { ...authHeader() },
    body: fd
  });
  if (!r.ok) throw new Error("Upload failed");
  return r.json();
}
