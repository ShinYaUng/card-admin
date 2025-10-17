import React, { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";

const API = import.meta.env.VITE_API_BASE?.replace(/\/+$/, "") || "";

export default function Login() {
  const nav = useNavigate();
  const { state } = useLocation();
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState("");

  async function onSubmit(e) {
    e.preventDefault();
    setErr("");
    setLoading(true);
    try {
      const res = await fetch(`${API}/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });
      const text = await res.text();
      if (!res.ok) throw new Error(tryJson(text)?.error || text || `HTTP ${res.status}`);
      const data = tryJson(text);
      localStorage.setItem("token", data.token);
      nav(state?.from?.pathname || "/cards", { replace: true });
    } catch (e) {
      setErr(e.message || "Login failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-wrap">
      <div className="login-card">
        <div className="login-head">
          <div className="logo" aria-hidden>🃏</div>
          <h1>Admin Login</h1>
          <p className="muted">API: {API || "— not set (VITE_API_BASE) —"}</p>
        </div>

        {err && (
          <div className="login-alert" role="alert">
            <strong>ไม่สำเร็จ:</strong> {err}
          </div>
        )}

        <form onSubmit={onSubmit} className="login-form">
          <label>
            <span>Username</span>
            <input
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="username"
              required
            />
          </label>

          <label>
            <span>Password</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </label>

          <button className="btn primary full" disabled={loading}>
            {loading ? "Logging in..." : "Login"}
          </button>
        </form>
      </div>
    </div>
  );
}

function tryJson(t) {
  try { return JSON.parse(t); } catch { return {}; }
}
