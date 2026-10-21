# Remote build, Unity license, and Feather

The project stays in GitHub. Unity and Xcode build on a remote macOS GitHub Actions runner. No game project or Asset Store package needs to be stored on the user's computer. The workflow creates an **unsigned** IPA and publishes it to GitHub Releases by default. Feather signs that IPA on the iPhone/iPad using a certificate configured inside Feather.

## 1. Add Unity build credentials to GitHub

Open the repository settings:

**[Repository Settings → Secrets and variables → Actions](https://github.com/mmzlopasa/founders-codex/settings/secrets/actions)**

Choose **New repository secret**. Add secrets according to the Unity plan on the account:

### Unity Personal

In Unity Hub, sign in to the Unity account that owns the project assets. Open **Preferences → Licenses → Add → Get a free personal license** and complete activation. GameCI uses the resulting license file. On Windows its default path is:

`C:\ProgramData\Unity\Unity_lic.ulf`

Open the `.ulf` file, copy its entire contents into the GitHub secret named `UNITY_LICENSE`, then also add:

- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

### Unity Plus/Pro

Add these three secrets instead:

- `UNITY_SERIAL` — serial from the Unity account subscription page
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

Do not add the activation request `.alf` file; the Personal workflow needs the completed `.ulf` license file. Do not send credentials or license contents in chat, commit them, or put them in normal GitHub variables. GitHub Actions secrets are write-only after saving.

## 2. Build and publish the unsigned IPA

After the Unity secrets are saved:

1. Open the repository’s **Actions** tab.
2. Choose **Build unsigned iOS IPA**.
3. Click **Run workflow**. Leave **Publish the unsigned IPA to GitHub Releases for import into Feather** enabled.
4. When the run succeeds, open **Releases** and download `Founders-The-Long-Rise-unsigned.ipa`.

The same IPA is available as a workflow artifact for 14 days. The Unity build and Xcode packaging run remotely; there is no Apple certificate or provisioning-profile secret in GitHub.

## 3. Sign and install through Feather

Feather does not make an unsigned IPA installable without signing. The app needs a valid signing certificate/profile pair that supports your iPhone or iPad. Feather’s documentation describes importing an IPA and signing it with the selected certificate.

1. In Feather, import the downloaded unsigned IPA into the app library.
2. In Feather’s certificate/signing settings, import the certificate `.p12` and its password, plus the matching `.mobileprovision` profile.
3. Select **Sign** for Founders, then install the signed app.
4. Keep the certificate and profile in Feather; do not upload them to this public repository or GitHub Actions.

If the profile is Ad Hoc, it must include the target device UDID. Certificate/profile provisioning is handled by the Apple signing provider/account you use with Feather, not by this project workflow.

## Asset Store packages

Adding an asset to a Unity account grants entitlement; it does not put package files in this GitHub checkout or make them available to a remote runner. The public repository must not contain licensed package binaries. The Editor catalog scans imported prefabs, terrain layers, materials, and UI sprites.

The remote Unity build still needs a permitted private asset-import/storage route for the required packages. Until then, it creates blockout prehistoric structures and uses the HUD fallback skin:

- [Ultimate Nature – Starter](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-nature-starter-176906)
- [EXPLORER – Stone Age](https://assetstore.unity.com/packages/3d/environments/historic/explorer-stone-age-150867)
- [Adventure UI Kit](https://assetstore.unity.com/packages/2d/gui/adventure-ui-kit-376222)
- [Viking Village | 3D Sample Project](https://assetstore.unity.com/packages/3d/environments/viking-village-3d-sample-project-29140)

The first successful build will reveal any remaining Unity/Xcode project issues. The repository has not yet produced an IPA.
