# Founders: The Long Rise

An iOS civilization-building game told through seasonal eras and live events.

## Status

The project baseline and first playable systems are being built from the plan in `docs/GAME_PLAN.md`.

## First season: Prehistoric Civilization

The first season focuses on founding a settlement, gathering resources, building a village, and expanding across a broad natural landscape. The world uses land and vegetation from [Ultimate Nature – Starter](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-nature-starter-176906) and prehistoric people, buildings, props, and animation direction from [EXPLORER – Stone Age](https://assetstore.unity.com/packages/3d/environments/historic/explorer-stone-age-150867).

The player can build anywhere on the map. A building clears vegetation and natural props under its footprint, including trees and rocks, then leaves the building in place. Tapping vegetation gathers food and wood; tapping rocks gathers stone. Buildings, cleared props, gathered resources, seasonal progress, and the season clock persist on the device.

## Project constraints

- The project and release artifacts belong in this GitHub repository.
- Do not create project files on the user's computer; Unity Hub is the only local Unity application.
- The target is iOS. The deliverable is a signed `.ipa` published in GitHub Releases when signing and build infrastructure are configured.
- Large source assets and Unity-generated caches must be handled separately from release binaries; do not commit generated caches.

## Current implementation

- A deterministic 64 × 64 chunk world (16.384 km square) with runtime terrain and nearby nature streaming.
- Free building placement on terrain, with trees and rocks removed under the building footprint.
- Tap vegetation and rocks to gather food, wood, and stone.
- Local device saves for buildings, gathered resources, cleared natural props, season clock, and event progress.
- A Unity Editor generator that scans imported assets, builds the prehistoric catalog, and creates the starter scene.
- A manual GitHub Actions workflow for remote iOS Xcode project export.

## Pending before an asset-complete IPA

- Make the two licensed Asset Store packages available to the remote Unity build without publishing their binaries in this public repo.
- Add Unity build and Apple signing secrets in GitHub Actions.
- Run and fix the first remote Unity build, then export and sign the Feather-installable IPA.

See [docs/GAME_PLAN.md](docs/GAME_PLAN.md) for the design and [docs/REMOTE_BUILD.md](docs/REMOTE_BUILD.md) for remote build setup. The current repository has not yet been built in Unity; generated scene and catalog content depend on the package import step.