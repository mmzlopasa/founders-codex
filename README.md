# Founders: The Long Rise

An iOS civilization-building game told through seasonal eras and live events.

## Status

This repository was empty when project work began. The initial baseline is being established in `docs/GAME_PLAN.md`.

## First season: Prehistoric Civilization

The first season focuses on founding a settlement, gathering resources, building a village, and expanding across a broad natural landscape. The world uses land and vegetation from [Ultimate Nature – Starter](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-nature-starter-176906) and prehistoric people, buildings, props, and animation direction from [EXPLORER – Stone Age](https://assetstore.unity.com/packages/3d/environments/historic/explorer-stone-age-150867).

The player can build anywhere on the map. A building clears vegetation and natural props under its footprint, including trees and rocks, then leaves the building in place. Tapping vegetation gathers food and wood; tapping rocks gathers stone. Buildings, cleared props, gathered resources, seasonal progress, and the season clock persist on the device.

## Project constraints

- The project and release artifacts belong in this GitHub repository.
- Do not create project files on the user's computer; Unity Hub is the only local Unity application.
- The target is iOS. The deliverable is a signed `.ipa` published in GitHub Releases when signing and build infrastructure are configured.
- Large source assets and Unity-generated caches must be handled separately from release binaries; do not commit generated caches.

## Development baseline

See [docs/GAME_PLAN.md](docs/GAME_PLAN.md) for gameplay, map, asset integration, seasons, technical approach, and staged deliverables.