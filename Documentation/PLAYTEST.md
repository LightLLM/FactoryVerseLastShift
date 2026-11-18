# Engine acceptance and walkthrough

## Expected route
Spawn at the aft observation deck, facing forward (+Z). The station runs from Z=-19 to +19. Robotics occupies the first bay, Engineering the second, Recovery the third, and the pod the final bay. Cyan deck strips mark the aisle. Green marks recovery and evacuation.

1. Walk to the supply trolley on the right (X=3, Z=-13). Aim at the arm; E grabs it. Carry to the orange socket on ATLAS (X=-3.05, Z=-12). Bring the held part near the socket and press E. Repeat for the cyan cell at the front of the robot.
2. Approach BOOT ATLAS at the left side of the assembly bench and hold LMB. Confirm the HUD changes to diagnosis, the alarm begins, and the robot follows.
3. Pass through the first hatch. Approach the engineering cabinet at X=-4.4, Z=-4. Scan the relay. Operate isolate, bypass and reboot. Try a wrong order first; verify an oxygen penalty, reset, and usable controls. A completed reboot must remove effective gravity from the player and canisters.
4. Move into Recovery (Z=4). Space rises, Ctrl descends, X brakes. Grab the three canisters individually and carry them to separate green sockets across the rack at Z=8. Confirm they stop drifting once latched, and only the third unlocks launch.
5. Pass through the final hatch to Pod 07. Enter the pod and hold its launch panel. The pod, player and ATLAS depart together during a six-second burn, then the victory card reports time, oxygen and errors.
6. Press R. The scene must return to its original title state with no retained sockets or mission state.

## Acceptance checks still requiring an activated Unity editor
- Scene imports without missing script references, pink materials, missing fonts, or package errors.
- Title screen, HUD and pause/end cards remain legible at 1280x720 and 1920x1080.
- All parts and panels can be targeted and operated through the camera ray and either virtual controller ray.
- Both simulated hands retain their independent offsets and reach when Tab switches hands.
- A wrong socket cannot accept a component. An occupied socket cannot double-count a component.
- Cargo remains in the pressure hull, drifts within reach and can be recovered in comfort mode.
- Floor, walls, hatches and pod are traversable without trapping the CharacterController.
- Zero-G thrust, braking, pause, resume and restart work in both input modes.
- Oxygen reserve is single-use; oxygen and timeout failures freeze progression.
- Escape flight and the robot companion remain visually coherent with no collision or camera problems.
- The generated station is visually inspected; audio levels are comfortable.
- A Windows build succeeds and a manual complete run works outside the editor.

## XR scope
The custom virtual-controller simulator is included and wired into the same grabbing, typed sockets and hold controls as desktop input. No OpenXR loader, real-headset rig, joint simulation, or XRI sample is included. These are separate device-integration tasks and must not be described as already tested.

For optional future Unity XRI integration, use a compatible XR Interaction Toolkit / Input System / OpenXR set, import Starter Assets and XR Device Simulator from Package Manager, configure an action-based XR Origin and replace the custom controller input. Forward select/hold events to the input-independent interaction methods and implement real tracked grabbable poses. The scaffold XRInputBridge remains an event adapter for ActionStation.Activate; it alone is not a tracked rig.

Unity references:
- https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/xr-device-simulator-overview.html
- https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/index.html
