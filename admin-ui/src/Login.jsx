import React, { useState } from "react";
import { login } from "./api.js";

export default function Login({ onLogin }) {
  const [username, setU] = useState("");
  const [password, setP] = useState("");
  const [err, setErr] = useState("");

  async function submit(e) {
    e.preventDefault();
    setErr("");
    try {
      const { token } = await login(username, password);
      onLogin(token);
    } catch (e) {
      setErr(e.message || "Login failed");
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: "72px auto", fontFamily: "sans-serif" }}>
      <h2>Admin Login</h2>
      <form onSubmit={submit}>
        <div>
          <label>Username</label><br/>
          <input value={username} onChange={e=>setU(e.target.value)} required />
        </div>
        <div style={{ marginTop: 8 }}>
          <label>Password</label><br/>
          <input type="password" value={password} onChange={e=>setP(e.target.value)} required />
        </div>
        {err && <p style={{ color: "crimson" }}>{err}</p>}
        <button style={{ marginTop: 12 }}>Login</button>
      </form>
    </div>
  );
}
