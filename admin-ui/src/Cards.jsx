import React, { useEffect, useRef, useState } from "react";
import { createCard, deleteCard, listCards } from "./api";
import "./theme.css";

const API = import.meta.env.VITE_API_BASE?.replace(/\/+$/, "") || "";

export default function Cards() {
  const [cards, setCards] = useState([]);
  const [editIndex, setEditIndex] = useState(null);
  const [form, setForm] = useState(defaultForm());
  const [msg, setMsg] = useState("");
  const fileRef = useRef(null);

  function defaultForm() {
    return {
      id: "",
      name: "",
      mana: 0,
      rarity: "Common",
      spriteUrl: "",
      actionType: "โจมตี",
      actionValue: 0,
      targetSide: "self",
      targetMode: "single",
      targetCount: 1,
    };
  }

  async function load() {
    const data = await listCards();
    setCards(data.cards || []);
  }
  useEffect(() => { load(); }, []);

  function onChange(e) {
    const { name, value, type, checked } = e.target;
    setForm((s) => ({ ...s, [name]: type === "checkbox" ? checked : value }));
  }

  // ===== Upload image to backend =====
  function onPickFile() { fileRef.current?.click(); }

  async function onFileChange(e) {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.type.startsWith("image/")) return alert("ไฟล์ต้องเป็นรูปภาพ");

    const fd = new FormData();
    fd.append("file", file);
    try {
      const res = await fetch(`${API}/upload`, { method: "POST", body: fd });
      const text = await res.text();
      if (!res.ok) throw new Error(text || `HTTP ${res.status}`);
      const { url } = JSON.parse(text);
      setForm((s) => ({ ...s, spriteUrl: url })); // เช่น /uploads/1711023_cat.png
    } catch (err) {
      alert("อัปโหลดไม่สำเร็จ: " + err.message);
    }
  }

  function clearImage() {
    setForm((s) => ({ ...s, spriteUrl: "" }));
    if (fileRef.current) fileRef.current.value = "";
  }

  async function onAdd() {
    if (!form.id || !form.name) return alert("กรอกข้อมูลให้ครบก่อนนะ!");
    try {
      await createCard({
        id: form.id,
        name: form.name,
        mana: Number(form.mana),
        rarity: form.rarity,
        spriteUrl: form.spriteUrl,          // URL จาก /upload หรือ URL ภายนอก
        action: {
          type: form.actionType,
          value: Number(form.actionValue),
          target: {
            side: form.targetSide,
            mode: form.targetMode,
            ...(form.targetSide === "enemy" ? { count: Number(form.targetCount) } : {}),
          },
        },
      });
      setMsg("✅ เพิ่มการ์ดสำเร็จ");
      setForm(defaultForm());
      await load();
    } catch (err) {
      alert("create failed: " + err.message);
    }
  }

  async function onDelete(id) {
    if (!confirm("ลบการ์ดนี้?")) return;
    await deleteCard(id);
    await load();
  }

  function onEdit(i) {
    setEditIndex(i);
    const c = cards[i];
    setForm({
      id: c.id,
      name: c.name,
      mana: c.mana,
      rarity: c.rarity,
      spriteUrl: c.spriteUrl || "",
      actionType: c.action?.type || "",
      actionValue: c.action?.value || 0,
      targetSide: c.action?.target?.side || "self",
      targetMode: c.action?.target?.mode || "single",
      targetCount: c.action?.target?.count || 1,
    });
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function onSaveEdit() {
    const newList = [...cards];
    newList[editIndex] = {
      id: form.id,
      name: form.name,
      mana: Number(form.mana),
      rarity: form.rarity,
      spriteUrl: form.spriteUrl,
      action: {
        type: form.actionType,
        value: Number(form.actionValue),
        target: {
          side: form.targetSide,
          mode: form.targetMode,
          ...(form.targetSide === "enemy" ? { count: Number(form.targetCount) } : {}),
        },
      },
    };
    setCards(newList);
    setEditIndex(null);
    setForm(defaultForm());
    setMsg("✏️ แก้ไขการ์ดเรียบร้อย (local)");
  }

  return (
    <div className="wrap">
      <h1>Manage Cards</h1>
      {msg && <div className="alert">{msg}</div>}

      <div className="card">
        <h2>{editIndex !== null ? "Edit Card" : "New Card"}</h2>

        <div className="grid">
          <label>
            Card ID
            <input name="id" value={form.id} onChange={onChange} />
          </label>
          <label>
            Name
            <input name="name" value={form.name} onChange={onChange} />
          </label>
          <label>
            Mana
            <input type="number" name="mana" value={form.mana} onChange={onChange} />
          </label>
          <label>
            Rarity
            <select name="rarity" value={form.rarity} onChange={onChange}>
              {["Common", "Uncommon", "Rare", "Epic", "Legendary"].map((r) => (
                <option key={r}>{r}</option>
              ))}
            </select>
          </label>

          <label className="span-2">
            Sprite URL
            <div className="row">
              <input
                name="spriteUrl"
                value={form.spriteUrl}
                onChange={onChange}
                placeholder="วางลิงก์รูป หรือกด Choose เพื่ออัปโหลด"
              />
              <button type="button" className="btn" onClick={onPickFile}>Choose</button>
              {form.spriteUrl && (
                <button type="button" className="btn danger" onClick={clearImage}>Clear</button>
              )}
            </div>
            <input
              ref={fileRef}
              type="file"
              accept="image/*"
              onChange={onFileChange}
              style={{ display: "none" }}
            />
            {form.spriteUrl ? (
              <div className="thumb-wrap">
                <img className="thumb" src={form.spriteUrl} alt="preview" />
              </div>
            ) : null}
          </label>
        </div>

        <h3>Action</h3>
        <div className="grid">
          <label>
            ประเภท
            <select name="actionType" value={form.actionType} onChange={onChange}>
              <option>โจมตี</option><option>ป้องกัน</option>
              <option>บัฟ</option><option>ฟื้นฟู</option>
            </select>
          </label>
          <label>
            ค่าตัวเลข
            <input type="number" name="actionValue" value={form.actionValue} onChange={onChange} />
          </label>
          <label>
            เป้าหมาย
            <select name="targetSide" value={form.targetSide} onChange={onChange}>
              <option value="self">ใส่ตัวเอง</option>
              <option value="enemy">ศัตรู</option>
            </select>
          </label>
          {form.targetSide === "enemy" ? (
            <>
              <label>
                โหมด
                <select name="targetMode" value={form.targetMode} onChange={onChange}>
                  <option value="single">1 ตัว</option>
                  <option value="random">สุ่ม</option>
                  <option value="all">ทั้งหมด</option>
                </select>
              </label>
              <label>
                จำนวนตัว
                <input type="number" name="targetCount" value={form.targetCount} onChange={onChange} min="1" />
              </label>
            </>
          ) : (
            <>
              <div className="placeholder" />
              <div className="placeholder" />
            </>
          )}
        </div>

        <div style={{ marginTop: 12 }}>
          {editIndex !== null ? (
            <button onClick={onSaveEdit}>💾 Save Edit</button>
          ) : (
            <button onClick={onAdd}>Add Card</button>
          )}
        </div>
      </div>

      <div className="card">
        <h2>Cards</h2>
        <table className="table">
          <colgroup><col/><col/><col/><col/><col/><col/><col/></colgroup>
          <thead>
            <tr>
              <th>ID</th><th>Name</th><th>Mana</th><th>Rarity</th>
              <th>Action</th><th>Target</th><th>Manage</th>
            </tr>
          </thead>
          <tbody>
            {cards.length ? (
              cards.map((c, i) => (
                <tr key={c.id}>
                  <td title={c.id}>{c.id}</td>
                  <td title={c.name}>{c.name}</td>
                  <td>{c.mana}</td>
                  <td>{c.rarity}</td>
                  <td>{c.action?.type} ({c.action?.value})</td>
                  <td>
                    {c.action?.target?.side} / {c.action?.target?.mode}
                    {c.action?.target?.count ? ` x${c.action.target.count}` : ""}
                  </td>
                  <td>
                    <button className="edit" onClick={() => onEdit(i)}>Edit</button>{" "}
                    <button className="danger" onClick={() => onDelete(c.id)}>Delete</button>
                  </td>
                </tr>
              ))
            ) : (
              <tr><td colSpan="7" className="empty">ไม่มีข้อมูลการ์ด</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
