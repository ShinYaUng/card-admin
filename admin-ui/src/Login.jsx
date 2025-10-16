import { useState } from "react";
import "./index.css";

export default function Login() {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const handleLogin = async () => {
        try {
            // เล่นเสียงคลิก (optional)
            new Audio("/click.mp3").play();

            const res = await fetch(`${import.meta.env.VITE_API_BASE}/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password }),
            });

            if (!res.ok) throw new Error("Failed to fetch");
            const data = await res.json();

            if (data.success) {
                alert("✨ Login Success! Welcome, " + username + " ✨");
                // Redirect หรือทำอย่างอื่นหลังล็อกอินได้ที่นี่
            } else {
                setError("Invalid username or password");
            }
        } catch (err) {
            setError("Failed to fetch");
        }
    };

    return (
        <div
            className="min-h-screen flex items-center justify-center bg-cover bg-center"
            style={{
                backgroundImage: "url('/bg-magic.jpg')",
            }}
        >
            <div className="container max-w-md bg-slate-900/70 border-2 border-purple-400 rounded-2xl p-8 shadow-[0_0_25px_rgba(180,100,255,0.6)] backdrop-blur-md">
                <h1 className="text-3xl text-center font-bold text-yellow-400 mb-6 drop-shadow-[0_0_10px_gold]">
                    🔮 Admin Login
                </h1>

                <label className="block text-purple-200 mb-2 font-semibold">
                    Username
                </label>
                <input
                    type="text"
                    placeholder="Enter your username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    className="w-full bg-slate-800/60 border border-yellow-500/50 rounded-lg px-3 py-2 text-slate-100 focus:ring-2 focus:ring-yellow-400 outline-none mb-4"
                />

                <label className="block text-purple-200 mb-2 font-semibold">
                    Password
                </label>
                <input
                    type="password"
                    placeholder="Enter your password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full bg-slate-800/60 border border-yellow-500/50 rounded-lg px-3 py-2 text-slate-100 focus:ring-2 focus:ring-yellow-400 outline-none mb-4"
                />

                {error && (
                    <div className="text-red-400 text-center font-semibold mb-3">
                        {error}
                    </div>
                )}

                <button
                    onClick={handleLogin}
                    className="btn-primary w-full py-2 mt-2 text-lg font-bold rounded-xl shadow-[0_0_15px_rgba(230,180,34,0.6)] hover:scale-105 hover:shadow-[0_0_25px_rgba(230,180,34,0.9)] transition-all bg-gradient-to-br from-yellow-500 to-purple-700"
                >
                    ✨ Login
                </button>

                <p className="text-center text-sm text-slate-300 mt-6 italic">
                    © 2025 ShinYa Card Master — Powered by Magic Deck System 🪄
                </p>
            </div>
        </div>
    );
}
