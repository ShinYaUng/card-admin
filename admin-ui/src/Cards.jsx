import React, { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

const API = import.meta.env.VITE_API_BASE?.replace(/\/+$/, "") || "";

function useAuthFetch() {
  return async (path, opts = {}) => {
    const token = localStorage.getItem("token") || "";
    const res = await fetch(`${API}${path}`, {
      ...opts,
      headers: {
        "Content-Type": "application/json",
        Authorization: token ? `Bearer ${token}` : undefined,
        ...(opts.headers || {}),
      },
    });
    if (res.status === 401) {
      // token หมดอายุ
      localStorage.removeItem("token");
    }
    return res;
  };
}

export default function Cards() {
  const nav = useNavigate();
  const authFetch = useAuthFetch();

  const [list, setList] = useState([]);
  const [q, setQ] = useState("");
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  const [form, setForm] = useState({
    id: "",
    cardName: "",
    manaCost: 0,
    rarity: "Common",
    spriteUrl: "",
    usableWithoutTarget: false,
    exhaustAfterPlay: false,
    actions: [{ type: "Attack", target: "Enemy", value: 8, delay: 0 }],
    desc: [{ text: "Deal {totalDamage} to an enemy.", modStat: "Strength", actionIndex: 0, useModifier: true }],
  });

  function logout() {
    localStorage.removeItem("token");
    nav("/login", { replace: true });
  }

  async function load() {
    setErr("");
    setLoading(true);
    try {
      const res = await authFetch("/cards");
      if (!res.ok) throw new Error(await res.text());
      const data = await res.json();
      setList(data.cards || []);
    } catch (e) {
      setErr(e.message || "Load failed");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const filtered = useMemo(() => {
    const k = q.trim().toLowerCase();
    if (!k) return list;
    return list.filter((c) => (c.cardName || "").toLowerCase().includes(k) || (c.id || "").includes(k));
  }, [q, list]);

  function updateForm(key, val) {
    setForm((s) => ({ ...s, [key]: val }));
  }

  async function addCard(e) {
    e.preventDefault();
    setErr("");
    try {
      const payload = { ...form };
      if (!payload.cardName) throw new Error("Card name required");
      const res = await authFetch("/cards", {
        method: "POST",
        body: JSON.stringify(payload),
      });
      if (!res.ok) throw new Error(await res.text());
      setForm((s) => ({ ...s, cardName: "", spriteUrl: "", manaCost: 0, id: "" }));
      await load();
      alert("Card added!");
    } catch (e) {
      setErr(e.message || "Add failed");
    }
  }

  async function removeCard(id) {
    if (!confirm("Delete this card?")) return;
    try {
      const res = await authFetch(`/cards/${encodeURIComponent(id)}`, { method: "DELETE" });
      if (!res.ok) throw new Error(await res.text());
      await load();
    } catch (e) {
      alert(e.message || "Delete failed");
    }
  }

  return (
    <div className="page">
      <header className="topbar">
        <div className="topbar__left">
          <h1 className="brand">Manage Cards</h1>
        </div>
        <div className="topbar__right">
          <button className="btn" onClick={load} title="Reload">Reload</button>
          <button className="btn danger" onClick={logout}>Logout</button>
        </div>
      </header>

      <main className="content grid gap">
        {/* New Card */}
        <section className="card">
          <h2 className="section-title">New Card</h2>
          <form className="form grid gap" onSubmit={addCard}>
            <div className="grid two">
              <label className="field">
                <span>Card ID (optional)</span>
                <input
                  placeholder="AUTO IF BLANK"
                  value={form.id}
                  onChange={(e) => updateForm("id", e.target.value)}
                />
              </label>
              <label className="field">
                <span>Name</span>
                <input
                  value={form.cardName}
                  onChange={(e) => updateForm("cardName", e.target.value)}
                  placeholder="Card name"
                  required
                />
              </label>
            </div>

            <div className="grid three">
              <label className="field">
                <span>Mana</span>
                <input
                  type="number"
                  min="0"
                  value={form.manaCost}
                  onChange={(e) => updateForm("manaCost", Number(e.target.value))}
                />
              </label>

              <label className="field">
                <span>Rarity</span>
                <select
                  value={form.rarity}
                  onChange={(e) => updateForm("rarity", e.target.value)}
                >
                  <option>Common</option>
                  <option>Uncommon</option>
                  <option>Rare</option>
                  <option>Epic</option>
                  <option>Legendary</option>
                </select>
              </label>

              <label className="field">
                <span>Sprite URL</span>
                <input
                  value={form.spriteUrl}
                  onChange={(e) => updateForm("spriteUrl", e.target.value)}
                  placeholder="https://...png"
                />
              </label>
            </div>

            <div className="grid two">
              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={form.usableWithoutTarget}
                  onChange={(e) => updateForm("usableWithoutTarget", e.target.checked)}
                />
                <span>Usable without Target</span>
              </label>

              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={form.exhaustAfterPlay}
                  onChange={(e) => updateForm("exhaustAfterPlay", e.target.checked)}
                />
                <span>Exhaust after Play</span>
              </label>
            </div>

            <details>
              <summary>Advanced (Actions & Descriptions)</summary>
              <div className="muted" style={{ marginTop: 8 }}>
                ใช้โครงสร้างเดิมที่ backend รองรับ – ฟอร์มนี้จะส่งค่า <code>actions</code> และ <code>desc</code> แบบเดิม
              </div>
              <pre className="code-block">{JSON.stringify({ actions: form.actions, desc: form.desc }, null, 2)}</pre>
            </details>

            {err && <div className="alert error">{err}</div>}
            <div className="actions">
              <button className="btn primary">Add Card</button>
            </div>
          </form>
        </section>

        {/* List */}
        <section className="card">
          <div className="section-title-row">
            <h2 className="section-title">Cards</h2>
            <input
              className="search"
              placeholder="Search by name or id…"
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </div>

          {loading ? (
            <div className="muted">Loading…</div>
          ) : (
            <div className="table">
              <div className="thead">
                <div>ID</div>
                <div>Name</div>
                <div>Mana</div>
                <div>Rarity</div>
                <div></div>
              </div>
              {filtered.map((c) => (
                <div className="trow" key={c.id}>
                  <div className="mono">{c.id}</div>
                  <div>{c.cardName}</div>
                  <div>{c.manaCost}</div>
                  <div>{c.rarity}</div>
                  <div className="right">
                    <button className="btn danger sm" onClick={() => removeCard(c.id)}>Delete</button>
                  </div>
                </div>
              ))}
              {!filtered.length && <div className="muted">No results</div>}
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
