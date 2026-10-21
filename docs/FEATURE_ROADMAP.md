# Gameplay roadmap

This is the implementation scope for **Founders: The Long Rise**. Foundation and Whiteout Survival are references for broad genre mechanics and player expectations. The game uses its own writing, tuning, interface layout, characters, and presentation.

## Required Unity Asset Store packages

1. [Adventure UI Kit](https://assetstore.unity.com/packages/2d/gui/adventure-ui-kit-376222) — visual source for the game HUD, panels, buttons, resource badges, icons, menus, and loading screen. Scan imported sprites into the runtime asset catalog and use the package’s visual language throughout the game.
2. [Viking Village | 3D Sample Project](https://assetstore.unity.com/packages/3d/environments/viking-village-3d-sample-project-29140) — source of modular settlement structures and scene props. Filter the catalog to buildings appropriate for the prehistoric season and adapt materials/presentation to the game’s first era.

The current Store listings identify Adventure UI Kit as a 15.3 MB free package with URP compatibility, and Viking Village as a 1.7 GB free sample project with URP compatibility. The latter is a large project source, so include only the licensed, selected assets needed by the game in the remote build. Do not copy package contents into this public repository. The listings are subject to change.

## First session and campfire progression

- Show a branded loading screen while the world and asset catalog initialize.
- On first launch, enter a short tutorial and ask the player to select any point on the map for the campfire.
- Make the campfire the settlement’s central progression object. Upgrades consume settlement resources, take time, and unlock additional building families, resident capacity, research, and additional construction slots.
- Keep the campfire location and its level in the persistent settlement save.

## Residents, work, and families

- Residents have assignable vocations: lumberjack, miner, farmer, rancher, builder, hunter, and caregiver.
- Each vocation has a level and experience. Work produces resources; repeated work improves its skill.
- Residents follow a daily schedule, satisfy needs, move between home and work, and use context-appropriate animation states.
- Family growth is represented abstractly and without sexual content. Adult households may welcome children when housing and food capacity permit. Children remain children, learn vocations through nonsexual mentoring with their parents, and become adults after a simulated growth period.
- Never depict children in sexual situations or sexualize them. The game’s family system is a population, care, and skill-transmission mechanic.

### Animation states

Use the imported humanoid rigs and animation clips after the packages are inventoried. The target state library includes idle, walk, run, carry, gather, chop, mine, sow, harvest, feed livestock, build, pray/celebrate, sit, sleep, and social interactions. Blend tree/state-machine setup must be checked against each imported rig rather than assuming all clips share one avatar.

## Construction, boosts, and economy

- Place the first campfire freely on any terrain.
- Route later buildings through a timed construction queue; show progress in the HUD and persist pending orders.
- Add consumable construction speedups and farming boosts. Make their effect and remaining count visible.
- Keep free terrain placement: vegetation and stones are cleared from a confirmed footprint and stay removed after chunk unloads or app restarts.
- Tie building costs, housing capacity, worker output, storage, and campfire unlocks into the same resource economy.

## Seasons, events, and research

- Season 1: **Prehistoric Civilization**, with Stone Age structures, residents, resources, and events.
- Seasonal events have dates, objectives, progress, and rewards. Claims must grant the listed reward to the save instead of showing a label only.
- A research tree unlocks better tools, farming, animal husbandry, building speed, and campfire upgrades.
- Keep season content data-driven so future eras can supply their own catalog, research, events, and visual theme.

## iOS product requirements

- Landscape iPhone/iPad layout, safe-area-aware and touch-first controls.
- Original app icon and loading screen using the Adventure UI Kit and game branding.
- Remote Unity build and Xcode archive on macOS; signed ad hoc IPA artifact for Feather installation.
- Keep certificates, provisioning profiles, Unity credentials, and licensed package binaries out of the public Git repository.

## Current code checkpoint

The repository now has a procedural streamed world, free terrain placement, device saves, resource gathering, a campfire-first placement flow, campfire levels and unlock rules, construction orders with consumable speedups, and a resident roster with jobs, skill levels, household mentoring, and a farming boost. The Editor catalog scans imported UI sprites.

The generated starter scene is not yet run in Unity, and no Asset Store content has been imported or catalogued by the remote build. The resident roster currently simulates production and skill data; visible citizen actors, their schedule-driven animations, tutorial, custom asset-backed HUD/loading screen, research, and reward claiming remain roadmap work.
