# Remote build and asset access

This project is kept in GitHub. Unity project files, imports, and builds are intended to run on a remote macOS GitHub Actions runner.

## iOS Xcode project export

Run **Actions → Export iOS Xcode project → Run workflow**. The workflow calls Unity's editor build method, creates the starter scene from the imported project assets, and uploads the generated Xcode project as a temporary GitHub Actions artifact.

Before a run, add these repository Actions secrets:

- `UNITY_LICENSE` — Unity license file contents
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

Store secret values only in GitHub Actions Secrets. Do not put them in the repository or chat. GameCI documents the Personal and Professional Unity license setup in its [activation guide](https://game.ci/docs/github/activation/).

## Asset Store packages

Adding an asset to a Unity account grants entitlement; it does not put the package files in this GitHub checkout. The asset catalog builder scans prefabs, terrain layers, and materials already imported under `Assets/`.

The source repository is public, so do not commit or publish the Unity Asset Store package files here. The Unity packages must be made available to the remote build through a permitted private import/storage path before the workflow can create the intended visual map. Until then, the scene builder can run with an empty asset catalog and only its procedural terrain fallback.

The referenced packages are:

- [Ultimate Nature – Starter](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-nature-starter-176906)
- [EXPLORER – Stone Age](https://assetstore.unity.com/packages/3d/environments/historic/explorer-stone-age-150867)

## Signed IPA

The current workflow exports the Unity iOS/Xcode project only. It does not create an installable `.ipa` yet. Producing a Feather-ready signed package requires an Apple signing certificate and a provisioning profile that covers the target device. Those are separate GitHub Actions secrets and must never be committed to the public repository.
