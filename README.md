# nullcore

A 2D platformer built in Unity.

## Requirements

- Unity **6000.5.8f1** (Unity 6)
- Open via Unity Hub, pointing at the `nullcore/` subfolder (the actual Unity project root)

## Scenes

- `MainMenu` — title screen (Start / Options), including a remappable keybindings grid with unsaved-changes protection
- `Level_1` — first playable level, floor + floating platforms, a spike hazard, and ability pickups

## Player

Movement, abilities, and keybindings all live under `Assets/Scripts`:

- **Movement**: walk/run (togglable default via Caps Lock, Shift as the modifier either way), jump, double jump, dash — all rebindable from the Options menu
- **Abilities are story-gated**: Double Jump, Dash, and Sprint each have a `Learned` flag (default `false`) that persists across scene loads for the rest of a play session. Pick up the corresponding `AbilityPickup` prefab in a level to unlock it.
- **HUD**: top-left ability icons only appear once an ability is learned, and flip between on/off art depending on whether it's currently usable
- **Keybindings**: rebindable via the Options menu (click a bound key, press a new one), persisted to `PlayerPrefs`, with a discard-changes confirmation if you navigate away without saving

## Project layout

```
Assets/
  Art/Sprites/       placeholder + pixel-art sprites (player, spikes, ability icons)
  Scenes/            MainMenu, Level_1
  Scripts/
    Camera/          camera follow
    Hazards/         spike hazard (respawns player on contact)
    Input/           keybind manager (remapping + persistence)
    Pickups/         ability pickups (grant a Learned flag on contact)
    Player/          player controller, abilities, ability progress
    UI/              main menu, keybindings UI, ability HUD
```

## Credits

- "Robot Sound" by Jordan Irwin (AntumDeluge), licensed under [OGA-BY 3.0](https://opengameart.org/content/robot-sound) — used as the player death sound effect (`Assets/Audio/SFX/player_death.ogg`).

## Editor tooling

This project is set up with the Unity MCP bridge (`com.unity.ai.assistant`) for AI-assisted editor automation. See `.mcp.json` at the repo root.
