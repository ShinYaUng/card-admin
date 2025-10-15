import React, { useEffect, useState } from "react";
import { listCards, deleteCard } from "./api.js";
import CardForm from "./CardForm.jsx";

export default function Cards({ onLogout }) {
  const [cards, setCards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editTarget, setEditTarget] = useState(null);

  async function refresh() {
    setLoading(true);
    try {
      const { cards } = await listCards();
      setCards(cards);
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => { refresh(); }, []);

  return (
    <div style={{ maxWidth: 1000, margin: "24px auto", fontFamily: "sans-serif" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h2>Manage Cards</h2>
        <div>
          <button onClick={refresh}>Reload</button>{" "}
          <button onClick={onLogout}>Logout</button>
        </div>
      </div>

      <CardForm onSaved={refresh} editData={editTarget} onCancel={()=>setEditTarget(null)} />

      <hr style={{ margin: "16px 0" }} />

      {loading ? <p>Loading...</p> : (
        <table width="100%" cellPadding="6" style={{ borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ textAlign: "left" }}>
              <th>ID</th><th>Name</th><th>Mana</th><th>Rarity</th><th>Sprite</th><th>Actions</th>
            </tr>
          </thead>
          <tbody>
          {cards.map(c => (
            <tr key={c.id} style={{ borderTop: "1px solid #ddd" }}>
              <td>{c.id}</td>
              <td>{c.cardName}</td>
              <td>{c.manaCost}</td>
              <td>{c.rarity}</td>
              <td>{c.spriteUrl ? <a href={c.spriteUrl} target="_blank">open</a> : "-"}</td>
              <td>
                <button onClick={()=>setEditTarget(c)}>Edit</button>{" "}
                <button onClick={async()=>{
                  if (!confirm("Delete this card?")) return;
                  await deleteCard(c.id);
                  refresh();
                }}>Delete</button>
              </td>
            </tr>
          ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
