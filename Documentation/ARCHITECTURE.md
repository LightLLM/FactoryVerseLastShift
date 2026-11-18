# Project architecture

StationBootstrap creates the world during Awake. StationWorld owns the procedural layout, generated materials, physical colliders, station labels, typed sockets, cargo, robot, camera and virtual controllers. The scene and build list are supplied in the project.

MissionDirector is the sole authority for phase, timer, suit oxygen, repair sequence, cargo count and delayed extraction. ActionStation requires hold-to-operate and only becomes used after success. AssemblySocket validates component type and occupancy before locking the Rigidbody and advancing assembly or recovery. Grabbable restores parts that escape the play volume. RobotAssembly records installed components.

PlayerRig uses a CharacterController for walking and inertial suit thrust. Both input modes share the ray interaction path. The custom XR simulator adds selectable left/right virtual controller origins, persistent independent hand offsets and reach, visual controller models and a ray. It is deliberately independent of external input packages.

GravitySystem removes gravity from the recoverable cargo and adds initial drift. Hull colliders and a soft vertical/longitudinal restoring force keep cargo recoverable. StationEffects drives emergency lighting, companion movement and pod departure. MissionHUD provides title, objective, clock, oxygen, interaction progress, pause and outcomes. Soundscape synthesizes the local hum, warning tones and interaction sounds.

RuntimeVerification is opt-in only (-fvqa or editor menu). The external logic suite compiles the actual MissionDirector and RobotAssembly sources against small Unity doubles. See the validation report for what was and was not executed.

To edit layout, change StationWorld and press Play again. To regenerate the bootstrap scene/build list, select FactoryVerse > Generate Last Shift Scene. World art is original procedural geometry created for this project. No remote services, analytics or API keys are required.
