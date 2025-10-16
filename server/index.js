// server/index.js
import express from "express";
import cors from "cors";
import dotenv from "dotenv";
import bcrypt from "bcrypt";
import jwt from "jsonwebtoken";
import multer from "multer";
import { v4 as uuid } from "uuid";
import path from "path";
import { fileURLToPath } from "url";

// ถ้ามี helper พวกนี้อยู่ในโปรเจกต์เดิม ให้ import ตามนี้
import { readDB, writeDB } from "./db.js";
import { authRequired } from "./auth.js";

dotenv.config();

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();

// --- CORS: อนุญาต localhost (dev) + โดเมน Vercel ของหน้า Admin ---
const ALLOWED_ORIGINS = [
  "http://localhost:5173",
  "https://card-admin-mu.vercel.app",
];

app.use(
  cors({
    origin: (origin, cb) => {
      if (!origin) return cb(null, true);
      cb(ALLOWED_ORIGINS.some((a) => origin.startsWith(a)) ? null : new Error("Not allowed by CORS"), true);
    },
    methods: ["GET", "POST", "PUT", "DELETE"],
    allowedHeaders: ["Content-Type", "Authorization"],
  })
);

app.use(express.json({ limit: "10mb" }));
app.use("/uploads", express.static(path.join(__dirname, "uploads")));

// === อัปโหลดไฟล์ (รูปการ์ด ฯลฯ) ===
const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, path.join(__dirname, "uploads")),
  filename: (req, file, cb) => {
    const ext = path.extname(file.originalname || ".png").toLowerCase();
    cb(null, `card_${Date.now()}_${Math.random().toString(36).slice(2)}${ext}`);
  },
});
const upload = multer({ storage });

// === seed แอดมิน (ครั้งแรก) ===
(function seedAdmin() {
  const db = readDB();
  db.users ||= [];
  if (db.users.length === 0) {
    const username = process.env.ADMIN_DEFAULT_USER || "admin";
    const password = process.env.ADMIN_DEFAULT_PASS || "admin123";
    const hash = bcrypt.hashSync(password, 10);
    db.users.push({ id: uuid(), username, passwordHash: hash, role: "admin" });
    writeDB(db);
    console.log(`[seed] admin: ${username} / ${password}`);
  }
})();

// ---------- AUTH ----------
app.post("/api/auth/login", (req, res) => {
  const { username, password } = req.body || {};
  const db = readDB();
  const user = db.users.find((u) => u.username === username);
  if (!user) return res.status(401).json({ error: "Invalid credentials" });
  if (!bcrypt.compareSync(password, user.passwordHash))
    return res.status(401).json({ error: "Invalid credentials" });

  const token = jwt.sign(
    { sub: user.id, username: user.username, role: user.role },
    process.env.JWT_SECRET || "dev-secret",
    { expiresIn: "12h" }
  );
  res.json({ token });
});

// ✅ ทางลัดเพื่อรองรับ frontend เก่าที่เรียก /login
app.post("/login", (req, res) => {
  const { username, password } = req.body || {};
  const db = readDB();
  const user = db.users.find((u) => u.username === username);
  if (!user) return res.status(401).json({ error: "Invalid credentials" });
  if (!bcrypt.compareSync(password, user.passwordHash))
    return res.status(401).json({ error: "Invalid credentials" });

  const token = jwt.sign(
    { sub: user.id, username: user.username, role: user.role },
    process.env.JWT_SECRET || "dev-secret",
    { expiresIn: "12h" }
  );
  res.json({ token });
});

// ---------- PUBLIC (ให้ Unity ใช้) ----------
app.get("/cards", (req, res) => {
  const db = readDB();
  res.json({ cards: db.cards || [] });
});

// ---------- ADMIN CRUD (ต้องมี token) ----------
app.get("/api/cards", authRequired, (req, res) => {
  const db = readDB();
  res.json({ cards: db.cards || [] });
});

app.post("/api/cards", authRequired, (req, res) => {
  const db = readDB();
  db.cards ||= [];
  const b = req.body || {};

  if (!b.cardName) return res.status(400).json({ error: "cardName required" });

  const card = {
    id: b.id || `CARD_${uuid()}`,
    cardName: b.cardName,
    manaCost: Number(b.manaCost || 0),
    rarity: b.rarity || "Common",
    spriteUrl: b.spriteUrl || "",
    usableWithoutTarget: !!b.usableWithoutTarget,
    exhaustAfterPlay: !!b.exhaustAfterPlay,
    actions: Array.isArray(b.actions) ? b.actions : [],
    desc: Array.isArray(b.desc) ? b.desc : [],
    upgrade: b.upgrade || null,
  };

  db.cards.push(card);
  writeDB(db);
  res.json({ ok: true, card });
});

app.put("/api/cards/:id", authRequired, (req, res) => {
  const db = readDB();
  const i = (db.cards || []).findIndex((c) => c.id === req.params.id);
  if (i === -1) return res.status(404).json({ error: "Not found" });
  db.cards[i] = { ...db.cards[i], ...req.body, id: db.cards[i].id };
  writeDB(db);
  res.json({ ok: true, card: db.cards[i] });
});

app.delete("/api/cards/:id", authRequired, (req, res) => {
  const db = readDB();
  const before = (db.cards || []).length;
  db.cards = (db.cards || []).filter((c) => c.id !== req.params.id);
  writeDB(db);
  res.json({ ok: true, removed: before - db.cards.length });
});

// อัปโหลดรูป -> คืน URL เก็บใน spriteUrl ได้เลย
app.post("/api/upload", authRequired, upload.single("file"), (req, res) => {
  const base = process.env.BASE_URL || `http://localhost:${process.env.PORT || 8080}`;
  const url = `${base}/uploads/${req.file.filename}`;
  res.json({ ok: true, url });
});

// ---------- START ----------
const port = process.env.PORT || 8080;
app.listen(port, "0.0.0.0", () => console.log(`✅ API running on :${port}`));
