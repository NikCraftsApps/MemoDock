# MemoDock

**MemoDock** is a clean, lightweight **clipboard manager for Windows** (WPF, .NET 8).  
It captures **text and images**, lets you **search**, **pin**, browse per-item **history (timeline)**, and keep a **live pinned window** — all **offline**, stored locally in SQLite. It minimizes to the **system tray** and can **auto-start** with Windows.

---

## 🌟 What's New in v1.0
- Instant capture of **text & images (PNG)** with auto-refresh  
- **Pin/Unpin**, **Pinned only** filter, and **Live pinned window**  
- **Timeline** per item (copy or restore previous versions)  
- Tray app: the close button hides to tray; exit via tray menu  
- **Import/Export** (JSON + files), retention & image size limits  
- Clean light UI; no cloud — everything local in `%LOCALAPPDATA%\MemoDock`

---

## ✨ Features
- Works with **text** and **images**
- **Search** within text + **Pinned only** filter
- Item actions: **Copy** · **History** · **Pin/Pinned**
- **Auto-refresh** (no manual refresh button)
- **Minimize to tray**, **Auto start** (HKCU\Run; no admin)
- **Import / Export** your library (portable JSON + files)
- **Retention**: limit for non-pinned items
- **Privacy-first**: 100% offline, no telemetry

---

## ⚡ Quick Start

### Requirements
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (devs only)

### Option A — Download & Run (users)
1. Get the latest ZIP from **Releases**:  
   `https://github.com/NikCraftsApps/MemoDock/releases`
2. Unzip anywhere (e.g., `C:\Apps\MemoDock\`).
3. Run `MemoDock.App.exe`.
4. The **X** button **hides** the window to tray. To quit, use tray menu → **Exit**.
5. Enable **Settings → Auto start** if you want it to launch with Windows.

> First run may trigger Windows SmartScreen (unsigned EXE). Click **More info → Run anyway**.

### Option B — Build from source (devs)
```bash
git clone https://github.com/NikCraftsApps/MemoDock.git
cd MemoDock
dotnet restore
dotnet run --project src/MemoDock.App/MemoDock.App.csproj
````

### Option C — Make a portable ZIP (self-contained EXE)

This creates a ZIP your users can unzip and double-click — no .NET runtime required.

**Publish (single line):**

```powershell
dotnet publish "src/MemoDock.App/MemoDock.App.csproj" -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:PublishReadyToRun=true -o "publish/memodock-win-x64"
```

**Create ZIP (choose one):**

* PowerShell:

  ```powershell
  mkdir "release" -ea 0
  Compress-Archive -Path "publish/memodock-win-x64/*" -DestinationPath "release/memodock-v1.0.0-win-x64.zip" -Force
  ```
* CMD (works on Win10/11):

  ```cmd
  mkdir release
  tar -a -c -f release\memodock-v1.0.0-win-x64.zip -C publish\memodock-win-x64 .
  ```

---

## 🧭 Quick Tour

1. **Copy** text/image anywhere → MemoDock captures it automatically
2. **Pin** important items → use **Pinned only** or **Live pinned window**
3. Click **History** on an item → see **timeline**, **Copy** or **Restore** a version
4. **Search** filters text content
5. **Settings**: auto start, size & retention limits, export/import

---

## 🗂 Data & Privacy

All data is stored locally:

```
%LOCALAPPDATA%\MemoDock\
  ├─ clips.db        (SQLite)
  ├─ store\          (copied images / large content)
  ├─ config.json     (app settings)
  └─ syncstate.json  (reserved for future sync)
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
