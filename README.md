# Template2D

Unity 6.2 template for 2D portrait mobile games (Built-in Render Pipeline).

Requires Unity 6000.2.6f1 and Git (Spine is installed as a git package).

## Getting started
1. Open the project and wait for packages to install.
2. Press Play in any scene. The game always starts from the Boot scene.

## Structure
- `Scripts/Core`: engine-level systems
- `Scripts/Common`: shared UI and tween presets
- `Scripts/Contexts`: app flow and scene code (Boot, Home, Game)

Dependencies go one way: Contexts → Common → Core.

## Systems
| System | Location |
|---|---|
| Boot and service setup | `Contexts/Application/Bootstrapper`, `Core/Services` |
| Scene loading (additive, loading screen) | `Core/SceneLoading`, `Contexts/Application/GameFlow` |
| Event bus | `Core/Events` |
| Pooling (warm/max counts from config) | `Core/Pooling` |
| Save and settings | `Core/Save`, `Core/Settings` |
| Audio | `Core/Audio` |
| State machine | `Core/StateMachine` |
| Localization | `Core/Localization`, `Common/UI/LocalizedText` |
| Haptics | `Core/Haptics` |
| Camera fit and safe area | `Core/CameraFit`, `Common/UI/SafeArea` |
| Popups and UI helpers | `Common/UI` |
| Tween presets | `Common/Tween` |
| Debug panel (F1 or three-finger tap) | `Core/Debugging` |

## Editor tools (`Tools > Project`)
- Always Start From Boot
- Pool Monitor
- Project Setup

## Packages
- PrimeTween
- Spine 4.3 (export to `Art/Spine/`)
- Unity MCP
