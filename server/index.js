// server/index.js
import "dotenv/config.js";
import express from "express";
import cors from "cors";
import jwt from "jsonwebtoken";
import fs from "fs";
import path from "path";
import multer from "multer";

const app = express();

// ===== Core middlewares =====
app.use(cors({ origin: true }));
app.use(express.json({ limit: "1mb" })); // ไม่ต้องใหญ่มาก เพราะไม่รับ base64 แล้ว
app.use(express.urlencoded({ extended: true, limit: "1mb" }));

// ===== Config =====
const PORT = Number(process.env.PORT || 8080);
const JWT_SECRET = process.env.JWT_SECRET || "dev_secret_change_me";
const ADMIN_USER = process.env.ADMIN_USER || process.env.ADMIN_DEFAULT_USER || "admin";
const ADMIN_PASS = process.env.ADMIN_PASS || process.env.ADMIN_DEFAULT_PASS || "admin123";

// ===== Health =====
app.get("/health", (_req, res) => res.json({ ok: true }));

// ===== Login (JWT) =====
app.post("/login", (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password) return res.status(400).json({ error: "username/password required" });
  if (username !== ADMIN_USER || password !== ADMIN_PASS) {
    return res.status(401).json({ error: "invalid credentials" });
  }
  const token = jwt.sign({ sub: username, role: "admin" }, JWT_SECRET, { expiresIn: "8h" });
  res.json({ token });
});

// ===== Simple file DB =====
const DB_FILE = path.join(process.cwd(), "data.json");
function ensureDB() {
  if (!fs.existsSync(DB_FILE)) fs.writeFileSync(DB_FILE, JSON.stringify({ cards: [] }, null, 2));
}
function readDB() { ensureDB(); return JSON.parse(fs.readFileSync(DB_FILE, "utf8")); }
function writeDB(data) { fs.writeFileSync(DB_FILE, JSON.stringify(data, null, 2)); }

// ===== Uploads (Production-like) =====
const UPLOAD_DIR = path.join(process.cwd(), "uploads");
if (!fs.existsSync(UPLOAD_DIR)) fs.mkdirSync(UPLOAD_DIR, { recursive: true });

// ให้โหลดไฟล์ได้ที่ /uploads/xxxx.png
app.use("/uploads", express.static(UPLOAD_DIR, {
  setHeaders: (res) => {
    res.setHeader("Cache-Control", "public, max-age=31536000, immutable");
  },
}));

// กรอง mimetype เฉพาะรูป
const imageFilter = (_req, file, cb) => {
  if (file.mimetype.startsWith("image/")) cb(null, true);
  else cb(new Error("only image files are allowed"));
};

const storage = multer.diskStorage({
  destination: (_req, _file, cb) => cb(null, UPLOAD_DIR),
  filename: (_req, file, cb) => {
    const safeBase = (path.parse(file.originalname).name || "img").replace(/[^a-zA-Z0-9_-]/g, "_");
    const ext = (path.extname(file.originalname) || ".png").toLowerCase();
    cb(null, `${Date.now()}_${safeBase}${ext}`);
  },
});

const upload = multer({
  storage,
  fileFilter: imageFilter,
  limits: { fileSize: 5 * 1024 * 1024 }, // 5MB
});

// POST /upload  field name: "file"  -> { url: "/uploads/xxxx.png" }
app.post("/upload", upload.single("file"), (req, res) => {
  if (!req.file) return res.status(400).json({ error: "no file" });
  const url = `/uploads/${req.file.filename}`;
  res.json({ url });
});

// ===== Cards API =====
app.get("/cards", (_req, res) => {
  const db = readDB();
  res.json({ cards: db.cards || [] });
});

app.post("/cards", (req, res) => {
  const p = req.body || {};
  const errors = [];
  if (!p.id || !String(p.id).trim()) errors.push("id is required");
  if (!p.name || !String(p.name).trim()) errors.push("name is required");
  if (p.spriteUrl && !/^\/uploads\//.test(p.spriteUrl) && !/^https?:\/\//.test(p.spriteUrl)) {
    errors.push("spriteUrl must be a valid URL or /uploads/*");
  }
  if (errors.length) return res.status(400).json({ errors });

  const db = readDB();
  db.cards = db.cards || [];
  if (db.cards.some((c) => c.id === p.id)) {
    return res.status(400).json({ errors: ["duplicated id"] });
  }

  db.cards.push({
    id: String(p.id).trim(),
    name: String(p.name).trim(),
    mana: Number(p.mana) || 0,
    rarity: p.rarity || "Common",
    spriteUrl: p.spriteUrl || "",          // เก็บ URL สั้น ๆ จาก /upload
    action: p.action || null,
  });
  writeDB(db);
  res.status(201).json({ ok: true });
});

app.delete("/cards/:id", (req, res) => {
  const db = readDB();
  const before = (db.cards || []).length;
  db.cards = (db.cards || []).filter((c) => c.id !== req.params.id);
  writeDB(db);
  res.json({ ok: true, removed: before - db.cards.length });
});

// ===== Start =====
app.listen(PORT, () => {
  console.log(`Server listening on http://localhost:${PORT}`);
});
