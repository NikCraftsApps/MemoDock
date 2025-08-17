# MemoDock

**MemoDock** is a clean, lightweight **clipboard manager for Windows** (WPF, .NET 8).  
It captures **text and images**, lets you **search**, **pin**, browse per-item **history (timeline)**, and keep a compact **Pinned window** — all **offline**, stored locally in SQLite. It minimizes to the **system tray** and can **auto-start** with Windows.

---

## 🚀 What’s new in **v1.1**
- **Multi-select** in the main list (checkbox + Extended selection)
- **Actions** menu: **Pin selected / Unpin selected / Delete selected** (with confirmation, no keyboard delete)
- **Drag & drop** reordering for **pinned** items (persistent `pin_order`)
- **Pinned Window** (renamed from Live Snippet) now supports **drag & drop** and **live refresh**
- Cross-window updates via **StorageEvents** (UI refresh after any data change)
- Safer startup: DB is initialized **before** clipboard capture starts
- **Log rotation** (keeps logs small and tidy)

> Next up for **1.2** (already in PRs): global hotkey (show/hide), UI polish, pause capture, duplicate collapsing, housekeeping.

---

## ✨ Features
- Works with **text** and **images (PNG)**
- **Search** within text + **Pinned only** filter
- Item actions: **Copy** · **History** · **Pin/Pinned**
- **Multi-select** + bulk **Actions**
- **Pinned list drag & drop** with stable order (`pin_order`)
- **Pinned Window** for quick access (drag & drop + live updates)
- **Minimize to tray**, optional **Auto start** (user-level, no admin)
- **Import / Export** (JSON + files)
- **Retention**: limit for non-pinned items
- **Privacy-first**: 100% offline, no telemetry

---

## ⚡ Quick Start

### Requirements
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (only for building from source)

### Option A — Download & Run
1. Grab the latest ZIP from **Releases**.
2. Unzip anywhere (e.g., `C:\Apps\MemoDock\`).
3. Run `MemoDock.App.exe`.
4. The **X** button hides to tray; to quit, use tray menu → **Exit**.
5. Enable **Settings → Auto start** if you want it to launch with Windows.

> First run may trigger Windows SmartScreen (unsigned EXE). Click **More info → Run anyway**.

### Option B — Build from source
```bash
git clone https://github.com/NikCraftsApps/MemoDock.git
cd MemoDock
dotnet restore
dotnet run --project src/MemoDock.App/MemoDock.App.csproj
````

### Option C — Publish + pack to ZIP (self-contained)

```powershell
dotnet publish "src/MemoDock.App/MemoDock.App.csproj" `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:PublishTrimmed=false `
  -o "publish/memodock-win-x64"

Compress-Archive -Path "publish/memodock-win-x64/*" -DestinationPath "memodock-vX.Y.Z-win-x64.zip" -Force
```

---

## 🧭 Quick Tour

1. Copy text/image anywhere → MemoDock captures it automatically.
2. **Pin** important items → filter with **Pinned only** or use the **Pinned Window**.
3. Click **History** on an item → see **timeline**, **Copy** or **Restore** a version.
4. Use **multi-select** and the **Actions** menu for bulk pin/unpin/delete.
5. **Settings**: auto start, size & retention limits, export/import.

---

## 🗂 Data & Privacy

All data is stored locally:

```
%LOCALAPPDATA%\MemoDock\
  ├─ clips.db        (SQLite)
  ├─ store\          (copied images / large content)
  ├─ config.json     (app settings)
  ├─ logs.txt / logs_*.txt (rotated logs)
  └─ ...
```

No external services, no telemetry.

---

## 🤝 Contributing

1. Fork the repo
2. Create a branch: `git checkout -b feature/name`
3. Commit: `git commit -m "feat: add ..."`
4. Push: `git push origin feature/name`
5. Open a Pull Request

---

## 📄 License

**MPL-2.0** — free to use, modify, and distribute. If you modify MemoDock’s source files, those modified files must be made available under MPL-2.0 (file-level copyleft). You can still use/link this project within proprietary apps without open-sourcing unrelated code. See `LICENSE` for full terms.

---

## 👤 Author

**Nikodem\_G**

---

**If you find this project helpful, please give it a ⭐ on GitHub!**
