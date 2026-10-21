# Remote build and asset access

This project is kept in GitHub. Unity imports and builds are intended to run on a remote macOS GitHub Actions runner. No game project or Asset Store package needs to be stored on the user's computer.

## Signed iOS IPA

Run **Actions → Build signed iOS IPA → Run workflow** after adding the Actions secrets listed below. The workflow creates the starter scene, generated app icon, and loading-screen background in the remote Unity Editor, exports the Xcode project, signs an ad hoc build, and uploads the `.ipa` as a temporary GitHub Actions artifact.

The optional `publish_release` checkbox creates a GitHub prerelease and attaches the IPA. Review a successful build before publishing it. An ad hoc profile must include the UDID of each device that should install the IPA through Feather.

For Unity/Xcode diagnostics without signing, run **Actions → Export iOS Xcode project → Run workflow** instead.

## Unity build secrets

Add these repository Actions secrets:

- `UNITY_LICENSE` — Unity license file contents
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

Store secret values only in GitHub Actions Secrets. Do not put them in the repository or chat. GameCI documents Personal and Professional Unity license setup in its [activation guide](https://game.ci/docs/github/activation/).

## Apple ad hoc signing secrets

Add these additional repository Actions secrets:

- `APPLE_CERTIFICATE_BASE64` — base64-encoded Apple Distribution `.p12` certificate
- `APPLE_CERTIFICATE_PASSWORD` — password for that certificate
- `APPLE_PROVISIONING_PROFILE_BASE64` — base64-encoded ad hoc `.mobileprovision` profile that includes the target device UDID
- `APPLE_TEAM_ID` — Apple Developer Team ID

The app bundle ID is `com.mmzlopasa.founderslongrise`. Apple requires an App ID, a distribution certificate, and registered devices for an ad hoc provisioning profile. See [Apple's ad hoc profile instructions](https://developer.apple.com/help/account/provisioning-profiles/create-an-ad-hoc-provisioning-profile/).

## Asset Store packages

Adding an asset to a Unity account grants entitlement; it does not put package files in this GitHub checkout. The Editor catalog scans imported prefabs, terrain layers, materials, and UI sprites.

The source repository is public, so do not commit or publish Asset Store package files here. Make the licensed packages available to the remote builder through a permitted private import or storage path before expecting their visuals in the game. Until then, the world catalog creates blockout prehistoric structures and the interface uses its fallback skin.

Required packages:

- [Ultimate Nature – Starter](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-nature-starter-176906)
- [EXPLORER – Stone Age](https://assetstore.unity.com/packages/3d/environments/historic/explorer-stone-age-150867)
- [Adventure UI Kit](https://assetstore.unity.com/packages/2d/gui/adventure-ui-kit-376222)
- [Viking Village | 3D Sample Project](https://assetstore.unity.com/packages/3d/environments/viking-village-3d-sample-project-29140)

## Release handling

The signed workflow retains an IPA artifact for 14 days. If `publish_release` is selected, it creates a GitHub prerelease with the IPA attached. Keep licensed Unity package binaries out of this public repository.
