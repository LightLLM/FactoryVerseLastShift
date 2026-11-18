# Devpost submission draft — FactoryVerse: Last Shift

Deadline: **Nov 18, 2026, 12:00 PM PST**. Everything below is a draft to review and edit; confirm each claim against the final build before submitting.

## Submission name
FactoryVerse: Last Shift

## Tagline (≤140 characters)
Build a robot with your bare hands, then escape a failing orbital factory — a seated, hands-first VR survival story for Meta Quest.

*(135 characters)*

## Track / Division
- **Track:** Gaming (narrative, seated, hands-first)
- **Division:** ⚠️ decide before submitting
  - *New Experience* — only if nothing of this project existed before **Sep 24, 2026**.
  - *Adapted / Significantly Updated* — if the earlier scaffold predates Sep 24. Significant update: **hand-tracking interaction (pinch-teleport, pinch-grab assembly) and a full Meta Quest VR mode**, plus the new FV-01 factory level, voiced dialogue and baked lighting moods.

## Description (≤500 words)

**Inspiration.** It's the last shift on FactoryVerse Station FV-01, an orbital robotics factory. You only have to finish one robot and go home — until the power bus overloads, gravity fails and the station starts coming apart. We wanted a short, complete VR story that someone can play in one sitting, on a plane or a bus, using nothing but their hands.

**What it is.** A 5–10 minute seated narrative game for Meta Quest:
1. **Assembly.** ARIA, the station AI, briefs you. Pinch the glowing power cell, carry it to Unit R-7's chest socket and let go. R-7 boots up, looks at you and cracks a dry joke.
2. **Catastrophic failure.** Alarms, red emergency lighting, Commander Hale on the radio: evacuate.
3. **Zero gravity.** The maintenance corridor loses gravity; tools drift past as you push toward the pod bay.
4. **Escape.** Board Pod 07 and launch while R-7 holds the connection — "It was an honour to serve."

**Hands-first, seated.** No controller is ever needed. You point at a glowing ring and pinch to move there (with a short comfort fade), and pinch to grab parts. Every interaction happens within arm's reach of a ring, so the whole game passes the airplane-seat test. A small hint floats above your hand until you've done each gesture once, so there's no wall of text. Controllers and desktop keyboard/mouse also work.

**How we built it.** All 3D assets — Unit R-7 (with separately installable head, arms and power core), workstations, consoles, the escape pod, a modular corridor kit, props, the observation deck and the station exterior — were modelled in Blender for this project and assembled in Unity 6 from a reference floor plan (assembly bay, control alcove, storage, secondary work area, observation deck, corridor, pod bay). Interaction uses OpenXR with Unity XR Hands (hand tracking + Meta hand-tracking aim). For Quest performance, each lighting mood (Normal, Emergency, Zero-G) is baked into its own lightmap set and swapped at runtime instead of using real-time lights, with static batching and occlusion culling. A dialogue manager plays 52 voiced lines across four characters with priority queuing (urgent ARIA warnings jump ahead; optional player lines never block) and head-locked subtitles for every line.

**What's next.** Two-handed assembly of R-7's arms and head with alignment markers, mixed-reality "station window" mode, more missions on the FV-01 station, and a daily "shift" challenge with time and oxygen scores.

## Hand interactions (optional field)
Pinch-to-teleport between glowing rings (aim ray from shoulder through palm, comfort fade), pinch-grab-carry-release to install parts into sockets (parts glide home if dropped, so nothing is ever lost), and hand-anchored onboarding hints. All built on OpenXR hand tracking via Unity XR Hands; tracked hands are rendered from joint data and hide the controller models automatically.

## Target launch date
TBD — e.g. Q1 2027 on the Meta Horizon Store (confirm).

## Team
Name / email / role for each member — fill in.

---

# Demo video shot list (< 3 minutes, real footage)

Record on the headset (Meta Quest casting/recording) or with the Meta XR Simulator. Lead with the strongest moments; judges may stop at 3:00.

| Time | Shot | Notes |
|---|---|---|
| 0:00–0:10 | Title card over the observation deck (Earth + station) | "FactoryVerse: Last Shift — a hands-first VR escape" |
| 0:10–0:35 | Hands appear; pinch-teleport across the assembly bay | Show the ring turning green and the fade |
| 0:35–1:05 | Pinch the power cell, carry it, release into R-7's chest; R-7 boots and looks at you | Keep ARIA + R-7 voice audible |
| 1:05–1:30 | Overload: lights snap to red, Hale on radio, subtitles | |
| 1:30–2:05 | Zero-G corridor: drifting tools, teleport along the corridor | |
| 2:05–2:40 | Pod bay, board the pod, countdown, R-7's farewell | |
| 2:40–2:55 | End card + "seated, hands-only, ~8 minutes" | Mention Meta Quest + hand tracking |

Upload to YouTube or Vimeo as **Public** and paste the link into the form.

---

# Upload checklist (Meta Developer Dashboard)
1. Create the app (if not yet) in the Meta Developer Dashboard → note the App ID.
2. Upload `Builds/Quest/FactoryVerse_LastShift.apk` to a **new release channel named "Competition"**.
3. Create an **Invite URL** for that channel and paste it into the Devpost form.
4. Keep the build available until the winner announcement (~Dec 11, 2026).
