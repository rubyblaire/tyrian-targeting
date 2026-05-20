# Tyrian Targeting

**GW2-inspired manual tab targeting for FFXIV.**

Tyrian Targeting is a Dalamud plugin for Final Fantasy XIV that refines manual target cycling with a Guild Wars 2-inspired feel. It improves how targets are filtered, sorted, cycled, marked, and recalled while keeping all targeting fully manual.

Tyrian Targeting does **not** auto-target, react to combat mechanics, or make combat decisions for the player. A target only changes when the player presses a keybind or command.

```text
https://raw.githubusercontent.com/rubyblaire/pluginmaster/main/pluginmaster.json
```
---

## Features

### Manual Tab Targeting

Tyrian Targeting replaces the feel of standard tab targeting with a configurable target cycle system.

Default controls:

- **TAB**: Cycle to next valid target
- **Shift + TAB**: Cycle to previous valid target
- **Ctrl + TAB**: Set current valid enemy as called target
- **Ctrl + Shift + TAB**: Clear called target

### Called Target

Inspired by Guild Wars 2’s called target behavior, Tyrian Targeting lets you save a priority enemy and quickly return to it.

The called target system supports:

- Set called target from current enemy
- Target called target with a configurable key
- Clear called target
- Show called target marker
- Reject friendly or unsupported targets

The default target-called key is **T**, but it can be changed in the plugin settings.

### Target Markers

Tyrian Targeting includes an original GW2-inspired marker overlay.

Marker options include:

- Current target marker
- Called target marker
- Soft target preview marker
- Theme-colored marker rendering
- Marker size
- World height offset
- Screen Y offset
- Opacity

The plugin does **not** include or bundle official Guild Wars 2 UI assets.

### Targeting Presets

Profiles are available at the top of the Targeting tab for quick swapping between common content styles:

- Default
- Dungeon
- Raid / Trial
- Fate / Overworld
- Safe Mode
- Custom

### Target Sorting

Targets can be sorted through different targeting styles, including:

- Distance
- Camera center
- Hybrid
- Left-to-right
- Current target relative
- Lowest HP
- Highest HP
- Combat-only

### Cache System

Tyrian Targeting uses a stable cycle cache so repeated Tab presses feel predictable instead of chaotic.

Cache options include:

- Cache duration
- Rebuild cache on target death
- Rebuild cache on camera movement
- Rebuild cache on player movement
- Keep current target in cycle

### Exclusions

Users can add target name fragments that Tyrian Targeting should ignore.

Example exclusions:

- Dummy
- Striking
- Carbuncle

### Themes

The plugin window includes Guild Wars 2 expansion-inspired theme colors:

- Core Tyria
- Heart of Thorns
- Path of Fire
- End of Dragons
- Secrets of the Obscure
- Janthir Wilds

The window uses an obsidian-black title bar with theme-colored tabs, buttons, checkboxes, inputs, and accents.

---

## Commands

| Command | Description |
| --- | --- |
| `/tt` | Open or close the Tyrian Targeting window. |
| `/tt help` | Show available commands. |
| `/tt next` | Manually cycle to the next valid target. |
| `/tt prev` | Manually cycle to the previous valid target. |
| `/tt nearest` | Target the nearest valid enemy. |
| `/tt clear` | Clear the current target cycle cache. |
| `/tt call` | Set your current valid enemy as the called target. |
| `/tt targetcall` | Target the current called target. |
| `/tt clearcall` | Clear the called target. |
| `/tt refresh` | Refresh the target list for debugging. |

---

## Important Notes

Tyrian Targeting is a manual targeting refinement plugin.

It does not:

- Auto-target enemies
- React to mechanics automatically
- Choose targets without player input
- Execute abilities
- Automate combat decisions
- Use official Guild Wars 2 assets

All targeting changes require direct player input through Tab, Shift+Tab, called-target keybinds, or slash commands.

---

## Links

- Discord: https://discord.gg/Dr836dmbqh
- Ko-fi: https://ko-fi.com/rubyblaire

---

## Author

Created by **Ruby Blaire**.
