# Checkers Online - Multiplayer Checkers Game 
A multiplayer online checkers game with a modern WPF interface and real-time communication using SignalR.

# Features 
- Online Battles — Play with friends in real-time

- Instant Updates — SignalR technology ensures move synchronization without delays

- Modern Interface — Clean and intuitive WPF client 

- Full Checkers Rules — Support for king promotion, and proper turn order

- Flexible Architecture — Separation of client and server logic for easy development

- Automatic Matchmaking — Room creation/connection system

# Architecture
The project uses a client-server architecture:

```Checkers Online
├── 📁 CheckersClient (WPF Application)
│   ├── Views/           - XAML views
│   ├── ViewModels/     - MVVM view models
│   ├── Services/       - Client services (SignalR, game logic)
│   └── Models/         - Client models
│
├── 📁 CheckersServer (ASP.NET Core + SignalR)
│   ├── Hubs/           - SignalR hub (CheckersHub)
│   ├── Services/       - Game logic (GameManager)
│   ├── Models/         - Server models
│   └── Program.cs      - Server configuration
│
└── 📁 CheckersModels (Shared Library)
    ├── Game/           - Shared game models
    ├── Board/          - Board and cell models
    └── Enums/          - Enumerations
```

# Installation & Running
1. Clone the repository

```
git clone https://github.com/your-username/checkers-online.git
cd checkers-online
```

2. Start the server

```
cd CheckersServer
dotnet run
```

3. Start the client

Open CheckersOnline.sln solution in Visual Studio --> Set CheckersClient as the startup project --> Press F5 to run --> In client settings, specify server address (default: https://localhost:7206)

4. Start playing

Create a new room or join by ID

Wait for the second player to connect

Start playing!
