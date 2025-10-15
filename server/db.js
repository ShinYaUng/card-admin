import fs from "fs";
const FILE = "./data.json";

function ensureFile() {
  if (!fs.existsSync(FILE)) {
    const initial = { users: [], cards: [] };
    fs.writeFileSync(FILE, JSON.stringify(initial, null, 2));
  }
}
export function readDB() {
  ensureFile();
  return JSON.parse(fs.readFileSync(FILE, "utf-8"));
}
export function writeDB(data) {
  fs.writeFileSync(FILE, JSON.stringify(data, null, 2));
}
