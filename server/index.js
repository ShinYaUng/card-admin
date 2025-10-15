import express from "express";
import cors from "cors";
import dotenv from "dotenv";
import bcrypt from "bcrypt";
import jwt from "jsonwebtoken";
import multer from "multer";
import { v4 as uuid } from "uuid";
import path from "path";
import { fileURLToPath } from "url";
import { readDB, writeDB } from "./db.js";
import { authRequired } from "./auth.js";

dotenv.config();
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
app.use(cors({
    origin: '*', // ✅ อนุญาตทุก origin (ใช้ใน LAN ได้)
    methods: ['GET', 'POST', 'PUT', 'DELETE'],
    allowedHeaders: ['Content-Type', 'Authorization']
}));
app.use(express.json({ limit: "10mb" }));

// เสิร์ฟไฟล์อัปโหลดแบบ static
app.use("/uploads", express.static(path.join(__dirname, "uploads")));

// === Storage อัปโหลดรูปแบบไฟล์ ===
const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, path.join(__dirname, "uploads")),
  filename: (req, file, cb) => {
    const ext = path.extname(file.originalname || ".png").toLowerCase();
    cb(null, `card_${Date.now()}_${Math.random().toString(36).slice(2)}${ext}`);
  }
});
const upload = multer({ storage });

// === Seed แอดมิน (ครั้งแรก) ===
(function seedAdmin() {
  const db = readDB();
  if (!db.users || db.users.length === 0) {
    const username = process.env.ADMIN_DEFAULT_USER || "admin";
    const password = process.env.ADMIN_DEFAULT_PASS || "admin123";
    const hash = bcrypt.hashSync(password, 10);
    db.users.push({ id: uuid(), username, passwordHash: hash, role: "admin" });
    writeDB(db);
    console.log(`[seed] admin: ${username} / ${password}`);
  }
})();

// === Auth ===
app.post("/api/auth/login", (req, res) => {
  const { username, password } = req.body || {};
  const db = readDB();
  const user = db.users.find(u => u.username === username);
  if (!user) return res.status(401).json({ error: "Invalid credentials" });
  if (!bcrypt.compareSync(password, user.passwordHash))
    return res.status(401).json({ error: "Invalid credentials" });

  const token = jwt.sign(
    { sub: user.id, username: user.username, role: user.role },
    process.env.JWT_SECRET,
    { expiresIn: "12h" }
  );
  res.json({ token });
});

// ===== Public route สำหรับ Unity =====
// รูปแบบ RemoteCardList { cards: RemoteCardDto[] }
app.get("/cards", (req, res) => {
  const db = readDB();
  res.json({ cards: db.cards || [] });
});

// ===== Admin: CRUD Cards (ต้องล็อกอิน) =====
app.get("/api/cards", authRequired, (req, res) => {
  const db = readDB();
  res.json({ cards: db.cards || [] });
});

app.post("/api/cards", authRequired, (req, res) => {
  const db = readDB();
  const body = req.body || {};
  if (!body.cardName) return res.status(400).json({ error: "cardName required" });
  const id = body.id || `CARD_${uuid()}`;
  const card = {
    id,
    cardName: body.cardName,
    manaCost: Number(body.manaCost || 0),
    rarity: body.rarity || "Common",
    spriteUrl: body.spriteUrl || "",         // ถ้าอัปโหลดรูปจะอัปเดตภายหลัง
    usableWithoutTarget: !!body.usableWithoutTarget,
    exhaustAfterPlay: !!body.exhaustAfterPlay,
    actions: Array.isArray(body.actions) ? body.actions : [],
    desc: Array.isArray(body.desc) ? body.desc : [],
    upgrade: body.upgrade || null
  };
  db.cards.push(card);
  writeDB(db);
  res.json({ ok: true, card });
});

app.put("/api/cards/:id", authRequired, (req, res) => {
  const db = readDB();
  const i = db.cards.findIndex(c => c.id === req.params.id);
  if (i === -1) return res.status(404).json({ error: "Not found" });

  db.cards[i] = { ...db.cards[i], ...req.body, id: db.cards[i].id };
  writeDB(db);
  res.json({ ok: true, card: db.cards[i] });
});

app.delete("/api/cards/:id", authRequired, (req, res) => {
  const db = readDB();
  const before = db.cards.length;
  db.cards = db.cards.filter(c => c.id !== req.params.id);
  writeDB(db);
  res.json({ ok: true, removed: before - db.cards.length });
});

// อัปโหลดรูป -> คืน spriteUrl ให้เอาไปใส่การ์ด
app.post("/api/upload", authRequired, upload.single("file"), (req, res) => {
  const url = `${process.env.BASE_URL || `http://localhost:${process.env.PORT||8080}`}/uploads/${req.file.filename}`;
  res.json({ ok: true, url });
});

app.listen(8080, "0.0.0.0", () => {
    console.log("API running on http://0.0.0.0:8080");
});