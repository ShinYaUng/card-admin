import React, { useEffect, useMemo, useState } from "react";
import { createCard, updateCard, uploadImage } from "./api.js";

const empty = {
  id: "", cardName: "", manaCost: 0, rarity: "Common",
  spriteUrl: "", usableWithoutTarget: false, exhaustAfterPlay: false,
  actions: [{ type:"Attack", target:"Enemy", value:8, delay:0 }],
  desc: [{ text:"Deal {totalDamage} to an enemy.", useModifier:true, actionIndex:0, modStat:"Strength" }],
  upgrade: null
};

export default function CardForm({ onSaved, editData, onCancel }) {
  const [form, setForm] = useState(empty);
  const isEdit = !!(editData && editData.id);

  useEffect(() => {
    setForm(isEdit ? {...empty, ...editData} : empty);
  }, [isEdit, editData?.id]);

  function setField(k, v) { setForm(prev => ({...prev, [k]: v})); }

  async function submit(e) {
    e.preventDefault();
    const payload = { ...form };
    if (!payload.cardName) return alert("cardName required");
    if (isEdit) await updateCard(payload.id, payload);
    else await createCard(payload);
    setForm(empty);
    onCancel?.();
    onSaved?.();
  }

  async function onUpload(e) {
    const f = e.target.files?.[0];
    if (!f) return;
    const { url } = await uploadImage(f);
    setField("spriteUrl", url);
  }

  const pretty = useMemo(()=>JSON.stringify(form, null, 2), [form]);

  return (
    <div style={{ background:"#f8f8f8", padding:12, borderRadius:8 }}>
      <h3>{isEdit ? "Edit Card" : "New Card"}</h3>
      <form onSubmit={submit}>
        <div style={{ display:"grid", gridTemplateColumns:"1fr 1fr 1fr 1fr", gap:8 }}>
          <label>Card ID (optional)
            <input value={form.id} onChange={e=>setField("id", e.target.value)} placeholder="auto if blank" />
          </label>
          <label>Name
            <input required value={form.cardName} onChange={e=>setField("cardName", e.target.value)} />
          </label>
          <label>Mana
            <input type="number" value={form.manaCost} onChange={e=>setField("manaCost", Number(e.target.value)||0)} />
          </label>
          <label>Rarity
            <select value={form.rarity} onChange={e=>setField("rarity", e.target.value)}>
              <option>Common</option><option>Rare</option><option>Epic</option><option>Legendary</option>
            </select>
          </label>
        </div>

        <div style={{ marginTop:8, display:"flex", gap:12, alignItems:"center" }}>
          <label>Sprite URL
            <input style={{ width:420 }} value={form.spriteUrl} onChange={e=>setField("spriteUrl", e.target.value)} />
          </label>
          <input type="file" accept="image/*" onChange={onUpload} />
          {form.spriteUrl && <a href={form.spriteUrl} target="_blank">preview</a>}
        </div>

        <div style={{ marginTop:8 }}>
          <label><input type="checkbox" checked={form.usableWithoutTarget} onChange={e=>setField("usableWithoutTarget", e.target.checked)} /> usableWithoutTarget</label>{" "}
          <label><input type="checkbox" checked={form.exhaustAfterPlay} onChange={e=>setField("exhaustAfterPlay", e.target.checked)} /> exhaustAfterPlay</label>
        </div>

        <fieldset style={{ marginTop:12 }}>
          <legend>Actions</legend>
          {form.actions.map((a, i)=>(
            <div key={i} style={{ display:"grid", gridTemplateColumns:"1fr 1fr 1fr 1fr 80px", gap:8, marginBottom:6 }}>
              <input value={a.type} onChange={e=>updAction(i,{...a, type:e.target.value})} placeholder="Attack/Block/..." />
              <input value={a.target} onChange={e=>updAction(i,{...a, target:e.target.value})} placeholder="Enemy/AllEnemies/..." />
              <input type="number" value={a.value} onChange={e=>updAction(i,{...a, value:Number(e.target.value)||0})} placeholder="value" />
              <input type="number" step="0.01" value={a.delay} onChange={e=>updAction(i,{...a, delay:Number(e.target.value)||0})} placeholder="delay" />
              <button type="button" onClick={()=>delAction(i)}>Del</button>
            </div>
          ))}
          <button type="button" onClick={()=>setForm(f=>({...f, actions:[...f.actions, {type:"Attack", target:"Enemy", value:5, delay:0}]}))}>+ Action</button>
        </fieldset>

        <fieldset style={{ marginTop:12 }}>
          <legend>Descriptions</legend>
          {form.desc.map((d, i)=>(
            <div key={i} style={{ display:"grid", gridTemplateColumns:"2fr 100px 100px 1fr 80px", gap:8, marginBottom:6 }}>
              <input value={d.text} onChange={e=>updDesc(i,{...d, text:e.target.value})} placeholder="text" />
              <label><input type="checkbox" checked={d.useModifier} onChange={e=>updDesc(i,{...d, useModifier:e.target.checked})}/> mod</label>
              <input type="number" value={d.actionIndex} onChange={e=>updDesc(i,{...d, actionIndex:Number(e.target.value)||0})}/>
              <input value={d.modStat} onChange={e=>updDesc(i,{...d, modStat:e.target.value})} placeholder="Strength/..." />
              <button type="button" onClick={()=>delDesc(i)}>Del</button>
            </div>
          ))}
          <button type="button" onClick={()=>setForm(f=>({...f, desc:[...f.desc, {text:"", useModifier:false, actionIndex:0, modStat:"Strength"}]}))}>+ Desc</button>
        </fieldset>

        <div style={{ marginTop:12 }}>
          <button type="submit">{isEdit ? "Save" : "Create"}</button>{" "}
          {isEdit && <button type="button" onClick={onCancel}>Cancel</button>}
        </div>
      </form>

      <details style={{ marginTop:12 }}>
        <summary>Preview JSON (ส่งไป API)</summary>
        <pre style={{ background:"#222", color:"#0f0", padding:12, borderRadius:6 }}>{pretty}</pre>
      </details>
    </div>
  );

  function updAction(i, v){ setForm(f=>({ ...f, actions: f.actions.map((x,idx)=>idx===i?v:x) })); }
  function delAction(i){ setForm(f=>({ ...f, actions: f.actions.filter((_,idx)=>idx!==i) })); }
  function updDesc(i, v){ setForm(f=>({ ...f, desc: f.desc.map((x,idx)=>idx===i?v:x) })); }
  function delDesc(i){ setForm(f=>({ ...f, desc: f.desc.filter((_,idx)=>idx!==i) })); }
}
