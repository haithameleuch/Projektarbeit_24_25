# Projektarbeit_24_25

Willkommen zur Projektarbeit 2024/25! Dieses Projekt ist inspiriert von *The Binding of Isaac* und kombiniert 3D-Objekte mit 2D-Spielmechaniken in einem sogenannten 2.5D-Stil. Der Spieler bewegt sich ausschließlich auf einer Ebene (ohne Sprünge) und interagiert mit einer dynamisch generierten Dungeon-Umgebung.

## Gruppenmitglieder
- Haitham El Euch
- Thorben Meiswinkel
- Arman Niaruhi
- Peter Kletschka
- Kai Schnieber

## Features
- Dungeon-Generierung.
- Charakter-Controller (First-Person- und Top-Down-Ansicht).
- Kamerawechsel (First-Person- und Top-Down-Ansicht).
- Öffnen und Schließen aller Türen.
- Schießen mit Projektilen.

>### Aktuelle Einschränkungen!
> **Projektile**:
>  - Momentan können Projektile sich unerwartet drehen. Dies ist ein bekannter Fehler und wird in zukünftigen Updates behoben.

## Verwendete Assets und Pakete
- **Cinemachine**: Für Kamerabewegung und Perspektivenwechsel.
- **Dungeon-Assets**: Objekte zur Raumerstellung.

## Projektstruktur
```markdown
Assets
├── AssetsDungeon
├── Materials
│   └── ProjectileColor
├── Prefabs
│   ├── Projectile
│   └── Room
├── Scenes
│   └── SampleScene
└── Scripts
    ├── Camera
    │   └── CameraController
    ├── Controller
    │   ├── FirstPersonPlayerController
    │   └── TopDownPlayerController
    ├── Dungeon
    │   ├── DungeonGenerator
    │   ├── OpenDoor
    │   ├── RoomBehaviour
    │   └── WallCollision
    ├── Manager
    │   ├── CameraManager
    │   ├── EventManager
    │   ├── GameInputManager
    │   └── ObjectPoolManager
    └── Shooting
        ├── PlayerShooting
        └── Projectile
```

## Anleitung

| Aktion               | Taste                 |
|-----------------------|-----------------------|
| Bewegung             | `W`, `A`, `S`, `D`   |
| Schießen             | `Spacebar`           |
| Ansicht wechseln     | `Tab`                |
| Türen öffnen         | Linksklick (`LMB`)   |
| Türen schließen      | Rechtsklick (`RMB`)  |
| Kamera steuern       | Mausbewegung (nur First-Person) |

## Installation und Start
1. **Unity-Version**: Stelle sicher, dass die richtige Unity-Version installiert ist. Diese Version wird für das gesamte Projekt verwendet.
   ```bash
   6000.0.27f1 LTS
   ```
3. **Projekt klonen**:
   ```bash
   git clone git@github.com:haithameleuch/Projektarbeit_24_25.git
   ```
4. **Branch wechseln**:
   ```bash
   git checkout base-functionality
   ```
3. **Projekt öffnen**: Spiel starten.