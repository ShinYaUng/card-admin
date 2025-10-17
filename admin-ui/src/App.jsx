import React from "react";
import { Routes, Route, Navigate, useLocation, useNavigate } from "react-router-dom";
import Login from "./Login.jsx";
import Cards from "./Cards.jsx";

const isAuthed = () => !!localStorage.getItem("token");

function ProtectedRoute({ children }) {
  const location = useLocation();
  if (!isAuthed()) return <Navigate to="/login" replace state={{ from: location }} />;
  return children;
}

function TopBar() {
  const navigate = useNavigate();
  function logout() {
    if (confirm("ต้องการออกจากระบบหรือไม่?")) {
      localStorage.removeItem("token");
      navigate("/login", { replace: true });
    }
  }
  return (
    <div
      style={{
        display: "flex", justifyContent: "space-between", alignItems: "center",
        background: "#fff", padding: "10px 20px",
        boxShadow: "0 2px 6px rgba(0,0,0,0.08)", position: "sticky", top: 0, zIndex: 50,
      }}
    >
      <h2 style={{ margin: 0 }}>🃏 Card Admin</h2>
      <button
        onClick={logout}
        style={{
          background: "linear-gradient(90deg,#ef4444,#f97316)",
          border: 0, color: "#fff", padding: "8px 14px",
          borderRadius: 10, fontWeight: 700, cursor: "pointer"
        }}
      >
        Logout
      </button>
    </div>
  );
}

export default function App() {
  return (
    <div className="app-shell">
      {isAuthed() && <TopBar />}
      <Routes>
        <Route path="/" element={<Navigate to="/cards" replace />} />
        <Route
          path="/cards"
          element={
            <ProtectedRoute>
              <Cards />
            </ProtectedRoute>
          }
        />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<Navigate to="/cards" replace />} />
      </Routes>
    </div>
  );
}
