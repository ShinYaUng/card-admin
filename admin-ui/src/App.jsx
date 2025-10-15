import React, { useState } from "react";
import Login from "./Login.jsx";
import Cards from "./Cards.jsx";

export default function App() {
  const [token, setToken] = useState(localStorage.getItem("token"));

  function onLogin(tok) {
    localStorage.setItem("token", tok);
    setToken(tok);
  }
  function onLogout() {
    localStorage.removeItem("token");
    setToken(null);
  }

  return token ? <Cards onLogout={onLogout} /> : <Login onLogin={onLogin} />;
}
