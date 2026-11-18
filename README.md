# FactoryVerse: Last Shift

A self-contained Unity 6 orbital-survival vertical slice, expanded from the supplied scaffold. All station geometry, material styling, starfield, robot and synthesized sound are generated locally at scene startup. No downloaded art or paid asset dependencies.

## Open and play
1. Extract this entire ZIP to a writable folder.
2. In Unity Hub choose **Add project from disk** and select the folder containing Assets, Packages and ProjectSettings.
3. Open with **Unity 6000.6.4f1**. Allow the built-in modules to resolve.
4. Open **Assets/FactoryVerse/Scenes/LastShift.unity** and press **Play**.
5. Choose **Begin Last Shift** or **Begin in XR Simulator**. Click the Game view if keyboard input is not focused.

The editor scene contains a bootstrap. The complete orbital station appears when Play starts. The source of the station layout is StationWorld.cs. This is intentional, not missing scene content.

## Play loop
- Robotics: collect the orange arm and cyan cell from the supply trolley. Fit each into its matching ATLAS socket. Hold BOOT ATLAS.
- Engineering: hold SCAN RELAY, then ISOLATE, BYPASS and REBOOT in that order. Errors cost oxygen and reset the sequence; previously operated controls become available again.
- Recovery: gravity fails. Use suit thrust, grab three drifting canisters and fit them into separate green rack sockets.
- Evacuation: enter Pod 07, hold LAUNCH ESCAPE POD for three seconds, then survive the six-second escape burn. ATLAS follows you after boot.
- Oxygen exhaustion or a twelve-minute orbital deadline ends the run. One emergency oxygen reserve adds up to 35 percentage points. Restart from pause or either ending.

## Controls
| Action | Desktop / built-in XR simulator |
|---|---|
| Move | WASD / arrow keys |
| Look / simulated head aim | Mouse |
| Grab, drop, insert matching component | E or right mouse (grip) |
| Operate control | Hold left mouse (trigger) or F |
| Zero-G ascend / descend | Space / left Ctrl |
| Zero-G brake | X |
| Toggle comfort speed | C |
| Switch desktop / XR simulator | F2 |
| Choose simulated left / right controller | Tab |
| Offset active controller | Hold middle mouse and move mouse |
| Extend / retract active controller | Mouse wheel |
| Pause / resume | Escape |
| Restart, including after failure | R |

Aim within 3.5 metres of parts or panels. In simulator mode the ray originates from the selected virtual controller. While carrying a part, move its held position within 1.25 metres of an unoccupied socket and press E / grip. Wrong component types are rejected. Controller offsets and reach are retained independently for each hand. This is a custom desktop controller simulator; it does not install Unity's XRI Device Simulator sample, emulate hand joints, or initialize a physical headset.

## Build and verify
- **FactoryVerse > Build Windows Player** writes Builds/Windows/FactoryVerse.exe and its required adjacent files.
- **FactoryVerse > Run Runtime Verification** runs scene-level socket and progression checks in Play mode and writes Validation/runtime-tests.txt.
- A built player supports `-fvqa` for the same runtime suite. Results go to the Unity persistent-data directory as FactoryVerse-runtime-tests.txt.
- Run `Tools/Verify-Logic.ps1` for independent mission logic checks with .NET 8.
- Run `Tools/Build-Windows.ps1` with the path to your activated Unity editor for a batch build.

## Validation status of this delivery
- The gameplay and editor sources compiled against the installed Unity 6000.6.4f1 engine/editor reference assemblies.
- **34 mission logic checks passed** against the actual MissionDirector source with lightweight Unity API test doubles. These verify state transitions and failure paths; they do not execute Unity physics or rendering.
- The native Unity editor launch failed during licensing initialization: the licensing IPC connection was refused and timed out. As a result, native scene import, rendered appearance, actual keyboard playthrough, runtime scene tests and a Windows executable build could not be verified here. No executable is included or claimed.
- Physical headset, OpenXR, Quest deployment, hand tracking and Unity XRI sample operation are not validated or configured. The included custom XR controller simulator requires no headset.

See Documentation/PLAYTEST.md for the remaining engine acceptance checklist. GitHub and Devpost were not modified.
