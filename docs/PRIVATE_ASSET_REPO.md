# Private Unity asset repository

The game project stays in the public `founders-codex` repository. Licensed Unity Asset Store source files must stay in a private repository and must not be committed to the public game repository.

## Repository expected by the build

Create a private repository named `mmzlopasa/founders-codex-assets`. Put the imported Unity content under this exact structure:

```text
Assets/
  StoreAssets/
    UltimateNature/
    VikingVillage/
    AdventureUIKit/
```

Keep Unity `.meta` files next to every imported file. The iOS workflow checks out this repository into a temporary runner directory and copies `Assets/StoreAssets` into the game project before Unity imports and builds it. The asset source is not committed to the public game repository or included in the diagnostic Xcode project.

## Connect the repositories

1. Create `founders-codex-assets` as a **private** GitHub repository.
2. In that private repo, enable Git LFS for large binary source files (for example `.fbx`, `.psd`, `.tif`, `.png`, `.wav`, and large `.unitypackage` files). Track Unity `.meta` files normally with Git.
3. Import the three packages into the Unity game project through Package Manager → My Assets. Move/copy the imported pack folders under the layout above while preserving all `.meta` files.
4. Commit and push those files to the private asset repo.
5. Create a fine-grained GitHub token with **Contents: Read-only** access limited to `founders-codex-assets`; add it to the public game repo as the Actions secret `FOUNDERS_ASSET_REPO_TOKEN`.

The workflow skips the private asset checkout until that secret exists. Once set, it expects the repository and folder layout above. Do not put Asset Store source files in the public repo or in public release artifacts.

## Unity package compatibility notes

- Ultimate Nature – Starter advertises URP compatibility with the project's Unity 6000.5.3f1.
- Viking Village advertises URP compatibility with Unity 6000.3.0f1 and its download is about 1.7 GB; inspect and import only what the game needs.
- Adventure UI Kit advertises Built-in and URP compatibility for Unity 2022.3.62. Import it in Unity 6 and resolve any package migration warnings before adopting its sprites and prefabs.

After import, run **Founders → Assets → Generate Asset Inventory**, then **Founders → World → Build Asset Catalog**, then **Founders → World → Create Prehistoric Starter Scene** in the Unity Editor. Inspect the generated scene and catalog before publishing a build.
