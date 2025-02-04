# T3-Jailbreak

A comprehensive CS2 Jailbreak plugin built with CounterStrikeSharp.

## Install / Dependecies
- T3Menu-API: **[** [**Download Here**](https://github.com/T3Marius/T3Menu-API/releases/tag/1.0.5) **]**
- CounteStrikeSharp: **[** [**Download Here**](https://github.com/roflmuffin/CounterStrikeSharp) **]**

# Upload
- Drag&Drop addons folder in game/csgo.
- You don't need to worry about the config file. It will automaticly update for any version without modifying your current settings.

## 🎮 Core Features

### Roles

#### 👮 Simon (Guard Leader)
- **Commands**:
  - `!s` - Become Simon
  - `!unsimon` - Resign from Simon
  - `!o / !c` - Control cell doors
  - `!color <player> <color>` - Color a player.
  - `!rebel <player>` - Mark as rebel
  - `!freeday <player>` - Grant freeday
  - `!box` - Starts box between prisoners (!box again to stop it.)
  - Simon can also cuffs a player with his tazer and move him while looking at him and holding RightClick.
  - Simon also can choose his Laser/Marker color from !smenu.

#### 🚔 Deputy
- **Commands**:
  - `!deputy` - Become deputy
  - `!undeputy` - Resign from deputy
  - Limited Simon permissions

### 🎯 Last Request System

Available LR:
- Knife Fight
- Shot for Shot
- Rebel
- Dodgeball
- Headshot Only
- Mag4Mag
- No Scope

### 🎉 Special Days

1. **One In The Chamber**
   - One bullet kills
   - Get ammo on kills

2. **No Scope Day**
   - Snipers only
   - No scoping allowed

3. **Hide And Seek**
   - Ts hide
   - CTs seek

4. **Teleport Day**
   - Random teleports
   - Survival game

5. **Arms Race**
   - Progressive weapons
   - Kill to advance

6. **Drunk Day**
   - Altered movement
   - Random effects

7. **War Day**
   - Team battle
   - Full loadout

## ⌨️ Commands

### Admin Commands
```css
!admin             - Open admin menu
!setsimon <player> - Force set Simon
!removesimon       - Remove current Simon
!sd                - Start special day
!setdeputy <player> - Force set Deputy
!removeddeputy     - Remove current Deputy
!lrtop             - Show top LR players
```

### HUD Messages
- Displays current Simon

### Database:
- In database it saves the players won/lost Last Requests which you can see in game with !lrtop.

### Cookies
- In cookies it saves player !guns settings so they can keep the same guns as CT.
- It saves the laser/marker color of each simon too.

### Tags
- Simon: ⭐ SIMON ⭐
- Deputy: ⭐ DEPUTY ⭐

### Config
```json
{
  "Database": {
    "DatabaseHost": "",
    "DatabaseName": "",
    "DatabaseUser": "",
    "DatabasePassword": "",
    "DatabasePort": 3306
  },
  "Simon": {
    "SetSimonIfNotAny": 10.0 // cooldown of choosing a new simon
  },
  "Models": {
    "SimonModel": "",
    "DeputyModel": "",
    "GuardModel": "",
    "ArmsRaceKnifeModel": ""
  },
  "LastRequest": {
    "EnableCheatPunishment": true, // right now this is usless since you can only damage with the gun from LR.
    "LrStartTimer": 5.0,
    "Types": {     // you can enable/disable any last requests from here.
      "KnifeFight": true,
      "ShotForShot": true,
      "NoScope": true,
      "MagForMag": true,
      "Dodgeball": true,
      "HeadShotOnly": true,
      "Rebel": true
    }
  },

  "SpecialDays": {
    "SDRoundsCountdown": 3,// how many rounds between special days
    "SdStartTimer": 15.0, // basic special days timer
    "HideTimer": 120.0, // timer for hide and seek day
    "WarTimer": 60.0, // timer for war day
    "ZombieTimer": 60.0, // timer for zombie(zombie disabled for now.)
    "ZombieHealth": 3500, // Zombies health
    "ZombieModel": "", // if you have a zombie model add it here.
    "AdminPermissions": [ // permission to use the admin jail commands
      "@css/generic"
    ],
    "Type": {     // you can enable/disable any specialday from here.
      "FreeForAll": true,
      "OneInTheChamber": true,
      "NoScope": true,
      "Teleport": true,
      "ArmsRace": true,
      "HideAndSeek": true,
      "DrunkDay": true,
      "WarDay": true
    }
  },
  "Commands": {
    "SimonMenu": [
      "wmenu",
      "smenu"
    ],
    "Simon": [
      "s",
      "simon"
    ],
    "UnSimon": [
      "us",
      "unsimon"
    ],
    "Deputy": [
      "d",
      "deputy"
    ],
    "UnDeputy": [
      "ud",
      "undeputy"
    ],
    "Box": [
      "box"
    ],
    "Ding": [
      "ding"
    ],
    "GunsMenu": [
      "gun",
      "guns"
    ],
    "OpenCells": [
      "open",
      "o"
    ],
    "CloseCells": [
      "close",
      "c"
    ],
    "ForgiveRebel": [
      "forgive",
      "pardon"
    ],
    "GiveUp": [
      "giveup",
      "p"
    ],
    "GiveFreeday": [
      "givefreeday",
      "freeday"
    ],
    "RemoveFreeday": [
      "removefreeday",
      "unfreeday"
    ],
    "LastRequest": [
      "lr",
      "lastrequest"
    ],
    "SpecialDays": [
      "sd",
      "specialday"
    ],
    "Heal": [
      "h",
      "heal"
    ],
    "SetColor": [
      "color",
      "setcolor"
    ],
    "LRTop": [
      "lrtop",
      "toplr"
    ],
    "QueueCommands": [
      "q",
      "queue"
    ],
    "QueueListCommands": [
      "qlist",
      "queuelist"
    ],
    "ExtendRoundTimeCommands": [
      "extend"
    ],
    "CTBanCommands": [
      "ctban"
    ],
    "AdminCommands": {
      "SetSimon": [
        "sw",
        "ss",
        "setsimon"
      ],
      "RemoveSimon": [
        "removes",
        "removesimon"
      ],
      "AdminPermissions": {
        "SetSimon": [
          "@css/generic"
        ],
        "RemoveSimon": [
          "@css/generic"
        ]
      }
    }
  },
  "BunnyHoop": {
    "BunnyHoopTimer": 20, // after how many seconds enable BunnyHoop on round start
    "PrintToCenterHtml": false, // print countdown to centerhtml
    "ShowChatMessages": true // show the message when enabled to chat.
  },
  "Prisoniers": {
    "SkipQueuePermissions": [ // automaticly in front of queue if have any of these flags
      "@css/vip"
    ],
    "HealCommandCountPerRound": 2 // how many times prisoners can ask for heal.
  },
  "Guardians": { // if you are guardian and have vip you can get x healthshot (not made yet.)
    "VipFlags": [
      "@css/vip"
    ],
    "GiveXHealthshot": [
      ""
    ]
  },
  // add sounds here if you want.
  "Sounds": {
    "SetSimonSound": "",
    "SimonDeathSound": "",
    "SimonGaveUpSound": ""
  },
  "Version": 1
}
```

## 🗺️ Roadmap

### Upcoming Features
1. **HUD Improvements**
   - Add prisoner count display
   - Add guardian count display
   - Improved visibility and positioning

2. **VIP Features**
   - Implement healthshot system for CT VIP players
   - Configurable through guardians settings

3. **Special Days Enhancement**
   - Complete Zombie Day implementation
   - Add proper zombie models and mechanics
   - Balance zombie health and abilities

4. **Code Optimization**
   - Code cleanup and restructuring
   - Performance improvements
   - Better documentation

5. **Creating API**
   - Creating api for the GetSimon, SetSimon, GetDeputy, SetDeputy, RemoveSimon, RemoveDeputy, etc.
