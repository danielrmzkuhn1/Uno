# UNO — Multiplayer LAN Desktop Game

A 2–4 player LAN implementation of the classic **UNO** card game featuring a **C# (.NET Windows Forms)** desktop client and game engine, a **Python FastAPI** REST & WebSocket server, and **MySQL** match persistence.

---

## Architecture & Tech Stack

- **Game Engine & Desktop UI (`UNO/`):** C# (.NET Framework, Windows Forms). Encapsulates deck management, turn order, and official UNO rule enforcement inside a self-contained game engine class decoupled from the UI layer.
- **Networking & API (`servidor/`):** Python & FastAPI.
  - **WebSocket Server:** Relays real-time player moves and synchronized game state across 2–4 clients over a local area network (LAN).
  - **REST API:** Handles player registration, match creation, move logging, winner recording, and player statistics.
- **Database (`base_datos/`):** MySQL relational schema tracking players, match histories, participants, and turn-by-turn move records.

---

## Key Features

- **2–4 Player LAN Multiplayer:** Real-time state synchronization over WebSockets with authoritative rule validation handled by the C# engine.
- **Complete UNO Rule Set:** Supports standard number cards, action cards (*Skip*, *Reverse*, *Draw Two*), and wild cards (*Wild*, *Wild Draw Four*), plus win-condition detection.
- **Match & Stats Persistence:** Automatically logs players, individual moves, and match outcomes to MySQL for post-game statistics.

---

## Repository Structure

```text
├── UNO/                  # C# Windows Forms client & core game logic
├── servidor/             # FastAPI REST endpoints & WebSocket relay server
├── base_datos/           # MySQL database scripts and schema
├── cliente_prueba.py     # Python test client for WebSocket/API debugging
└── servidor_prueba.py    # Standalone server test harness
