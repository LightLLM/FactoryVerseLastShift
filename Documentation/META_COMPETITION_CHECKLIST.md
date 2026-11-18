# FactoryVerse: Last Shift — Meta VR Start Developer Competition 2026 readiness

Deadline: **Nov 18, 2026, 12:00 PM PST**. Updated Nov 17, 2026.

**This is the shipping project** (created by Meta Quest Developer Hub): Unity 6000.6.9f1, URP 17, Meta XR SDK 209 (MetaXRFeature on OpenXR), XR Hands 1.10, Input System only. The older built-in-pipeline project in `FactoryVerse-LastShift-Unity6` is superseded.

Migration notes:
- All materials converted to URP Lit on import (`ModelMaterialSetup.ToRenderPipeline`); scene rebuilt and the three lighting moods re-baked.
- Android only allows the Input System, so desktop/debug keys go through `FVInput` (Scripts/Core) instead of `UnityEngine.Input`.
- OpenXR (Android): MetaXRFeature + Meta Hand Tracking Data Source + Unity **Hand Tracking Subsystem** (required by the data source) + foveation/SpaceWarp + Touch profiles.
- Meta project config: Hand Tracking Support = **Controllers and Hands** (writes the hand-tracking manifest entries).
- `Editor/StripUnusedAndroidPermissions.cs` removes microphone/Bluetooth permissions that the template packages pull in.

## Hard requirements

| Requirement | Status | Notes |
|---|---|---|
| VR app for Meta Quest, delivered as an **APK** | ✅ Built | `Builds/Quest/FactoryVerse_LastShift.apk` (~77 MB; Meta SDK native libs). App name "FactoryVerse: Last Shift", package `com.factoryverse.lastshift`, version 1.0.0, arm64-v8a, min API 32, IL2CPP, Vulkan/GLES3, ASTC. Build with **FactoryVerse → XR → Build Quest APK** (bumps versionCode each time; an IL2CPP build takes ~30 min). |
| **Hands-first**: completable without a controller | ✅ Implemented, ⚠️ not yet tested on a headset | Meta hand tracking (Controllers and Hands) feeding XR Hands. Manifest has `com.oculus.permission.HAND_TRACKING` and `oculus.software.handtracking`. Point-and-pinch teleport rings cover every story beat; pinch-grab-carry-release installs the power cell; the demo restarts itself at the end. |
| APK in release channel **"Competition"** + Invite URL | ❌ Your action | Meta Developer Dashboard → app → upload APK → new channel "Competition" → Invite URL. |
| Demo video < 3 min, public YouTube/Vimeo | ❌ Your action | Shot list in `DEVPOST_SUBMISSION.md`. Real headset or XR Simulator footage. |
| Devpost form | ⚠️ Draft ready | `Documentation/DEVPOST_SUBMISSION.md` (name, tagline, description, hands section). Decide the **Division**. |
| English / subtitles | ✅ | Every voiced line has a head-locked subtitle. |
| Original content, no brands/real people | ✅ | Blender-built assets. Voices are placeholders (Windows speech) until ElevenLabs is available — check ElevenLabs licence terms before shipping its audio. |

## Design guidelines

| Guideline | Status |
|---|---|
| Seated / airplane-seat test | ✅ Teleport rings + in-reach interactions; no physical walking needed. |
| Fast in/out, pause/resume, ≤10 min | ✅ Pause on headset removal/system menu (time + audio). Demo ~6–8 min with voice. Auto-restart. |
| First five minutes / no wall of text | ✅ Hand-anchored hints ("Point at a glowing ring then pinch to move", "Pinch to grab") until each gesture is used; comfort fade on teleport. |
| FoV-aware UI | ⚠️ Objective + subtitles are head-locked near the centre; verify comfort on device. |
| Quest performance | ⚠️ Baked lightmaps per mood, static batching, occlusion culling, compressed mono voice clips. **Not measured on device** — check with OVR Metrics Tool (target 72 fps). |

## Still to do / verify on a headset
1. Install: `adb install -r Builds/Quest/FactoryVerse_LastShift.apk` (Quest in developer mode, USB). Play the full loop with hands only.
2. Tune pinch thresholds (2 cm on / 3.5 cm off), ray feel and ring sizes if needed (FactoryHandRig fields).
3. Check frame rate in the bay and corridor; if under 72 fps, lower lightmap resolution or remove the Earth backdrop mesh.
4. Replace placeholder voices with ElevenLabs takes (same file names in `Assets/FactoryVerse/Resources/Dialogue/`).
5. Upload, Invite URL, video, Devpost form before the deadline.
