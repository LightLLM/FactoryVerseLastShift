using UnityEngine;
namespace FactoryVerse {
public static class StationWorld {
 static Material hull,dark,cyan,orange,green,white,red;static MissionDirector mission;
 public static GameObject Root;
 public static GameObject Cube(string name,Vector3 p,Vector3 s,Material mat,Transform parent=null){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent?parent:Root.transform);o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
 static Material Mat(string name,Color color,bool glow=false){var template=Resources.Load<Material>("FactoryVerseStandard");var m=template?new Material(template):new Material(Shader.Find("Standard"));m.name=name;m.color=color;m.SetFloat("_Glossiness",.45f);m.SetFloat("_Metallic",.35f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.6f);}return m;}
 static GameObject Sphere(string name,Vector3 p,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;o.transform.SetParent(Root.transform);o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
 static void Label(string text,Vector3 p,float size,Color color,Quaternion? rotation=null){var go=new GameObject(text);go.transform.SetParent(Root.transform);go.transform.position=p;go.transform.rotation=rotation??Quaternion.identity;var t=go.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<Renderer>().sharedMaterial=t.font.material;t.text=text;t.characterSize=size;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.color=color;}
 static ActionStation Panel(string name,string action,Vector3 p,Material color,float hold=1.2f){var o=Cube(name,p,new Vector3(1.4f,.72f,.22f),color);var a=o.AddComponent<ActionStation>();a.Mission=mission;a.Action=action;a.Title=name;a.HoldSeconds=hold;Label(name,p+Vector3.back*.14f,.065f,Color.white);return a;}
 static Grabbable Part(string kind,string title,Vector3 p,Vector3 scale,Material m){var o=Cube(title,p,scale,m);var rb=o.AddComponent<Rigidbody>();rb.mass=.5f;rb.linearDamping=.3f;rb.angularDamping=.25f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;var g=o.AddComponent<Grabbable>();g.Kind=kind;g.Title=title;return g;}
 static AssemblySocket Socket(string kind,Vector3 p,Material color,RobotAssembly robot=null){var o=Cube(kind.ToUpper()+" SOCKET",p,new Vector3(.65f,.65f,.25f),color);var s=o.AddComponent<AssemblySocket>();s.Kind=kind;s.Mission=mission;s.Robot=robot;s.SnapPoint=o.transform;if(robot)o.transform.SetParent(robot.transform,true);Label(kind.ToUpper()+" SOCKET",p+new Vector3(0,.48f,-.16f),.055f,Color.white);return s;}
 public static void Create(){
 Root=new GameObject("FactoryVerse / Orbital Foundry 09");
 hull=Mat("Titanium blue",new Color(.1f,.16f,.22f));dark=Mat("Obsidian",new Color(.025f,.045f,.07f));cyan=Mat("Ion cyan",new Color(.03f,.72f,.9f),true);orange=Mat("Safety amber",new Color(1,.34f,.06f),true);green=Mat("Recovery mint",new Color(.12f,.85f,.45f),true);white=Mat("Ceramic",new Color(.7f,.8f,.85f));red=Mat("Warning",new Color(1,.07f,.08f),true);
 mission=new GameObject("Mission Director").AddComponent<MissionDirector>();mission.gameObject.transform.SetParent(Root.transform);
 RenderSettings.ambientLight=new Color(.16f,.23f,.32f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.015f,.035f,.065f);RenderSettings.fogDensity=.012f;
 var sun=new GameObject("Solar rim light").AddComponent<Light>();sun.transform.SetParent(Root.transform);sun.type=LightType.Directional;sun.intensity=1.1f;sun.color=new Color(.65f,.8f,1);sun.transform.rotation=Quaternion.Euler(40,-35,0);
 Cube("Pressure floor",new Vector3(0,-.2f,0),new Vector3(16,.4f,38),hull);
 Cube("Pressure ceiling",new Vector3(0,5.3f,0),new Vector3(16,.3f,38),dark);
 Cube("Port hull",new Vector3(-8,2.5f,0),new Vector3(.3f,5.5f,38),hull);
 // Starboard panoramic window is collider-only: unobstructed view into space.
 var window=Cube("Panoramic force glass",new Vector3(8,2.5f,0),new Vector3(.15f,5.5f,38),dark);window.GetComponent<Renderer>().enabled=false;
 Cube("Aft bulkhead",new Vector3(0,2.5f,-19),new Vector3(16,5.5f,.3f),hull);Cube("Forward bulkhead",new Vector3(0,2.5f,19),new Vector3(16,5.5f,.3f),hull);
 for(int z=-18;z<=18;z+=3){Cube("Deck seam",new Vector3(0,.015f,z),new Vector3(16,.025f,.035f),dark);Cube("Ceiling rib",new Vector3(0,5,z),new Vector3(16,.35f,.2f),hull);Cube("Window rib",new Vector3(7.9f,2.5f,z),new Vector3(.3f,5,.2f),hull);for(int side=-1;side<=1;side+=2){Cube("Guidance strip",new Vector3(side*6.6f,.025f,z),new Vector3(.05f,.03f,2.5f),cyan);Cube("Overhead light",new Vector3(side*4,4.95f,z),new Vector3(1.4f,.06f,.13f),white);}}
 for(int z=-8;z<=10;z+=9){Cube("Bulkhead left",new Vector3(-5.5f,2.4f,z),new Vector3(5,4.8f,.4f),hull);Cube("Bulkhead right",new Vector3(5.5f,2.4f,z),new Vector3(5,4.8f,.4f),hull);Cube("Hatch crown",new Vector3(0,4.5f,z),new Vector3(6,.7f,.5f),dark);for(int i=-1;i<=1;i+=2)Cube("Hatch trim",new Vector3(i*2.95f,2.1f,z-.23f),new Vector3(.08f,4.2f,.08f),cyan);}
 Label("09 / ORBITAL FOUNDRY",new Vector3(0,3.9f,-18.8f),.2f,Color.cyan,Quaternion.Euler(0,180,0));
 Label("01   /   ROBOTICS",new Vector3(0,3.9f,-8.3f),.16f,Color.cyan);
 Label("02   /   ENGINEERING",new Vector3(0,3.9f,.7f),.16f,Color.cyan);
 Label("03   /   RECOVERY",new Vector3(0,3.9f,9.7f),.16f,Color.cyan);
 Label("04   /   EVACUATION",new Vector3(0,3.9f,18.7f),.16f,Color.green);
 // Robot workshop, with reachable table and separate typed sockets.
 Cube("Assembly workbench",new Vector3(-3.8f,.55f,-12),new Vector3(5,1.1f,1.5f),dark);
 var robot=new GameObject("ATLAS / service robot").AddComponent<RobotAssembly>();robot.transform.SetParent(Root.transform);mission.Robot=robot;
 Cube("ATLAS torso",new Vector3(-4,1.8f,-12),new Vector3(1,1.2f,.65f),white,robot.transform);Cube("ATLAS head",new Vector3(-4,2.7f,-12),new Vector3(.75f,.5f,.65f),dark,robot.transform);Cube("ATLAS visor",new Vector3(-4,2.72f,-12.34f),new Vector3(.56f,.12f,.03f),cyan,robot.transform);
 Socket("arm",new Vector3(-3.05f,1.9f,-12),orange,robot);Socket("cell",new Vector3(-4,1.6f,-12.5f),cyan,robot);
 Cube("Parts trolley",new Vector3(3,.55f,-13),new Vector3(3,1.1f,1.5f),dark);Part("arm","ATLAS ARM",new Vector3(2.4f,1.5f,-13),new Vector3(.42f,.75f,.42f),orange);Part("cell","POWER CELL",new Vector3(3.5f,1.45f,-13),new Vector3(.4f,.6f,.4f),cyan);
 Label("COMPONENT SUPPLY",new Vector3(3,2.4f,-13),.09f,Color.cyan);Panel("BOOT ATLAS","assemble",new Vector3(-5.8f,1.6f,-11.6f),green);
 // Relay is intentionally accessible from the centre aisle.
 Cube("Relay cabinet",new Vector3(-4.4f,1.6f,-4),new Vector3(5,3.2f,1),dark);
 Panel("SCAN RELAY","diagnose",new Vector3(-4.4f,2.55f,-4.6f),cyan,2);
 Panel("ISOLATE","isolate",new Vector3(-6,1.45f,-4.6f),orange);Panel("BYPASS","bypass",new Vector3(-4.4f,1.45f,-4.6f),cyan);Panel("REBOOT","reboot",new Vector3(-2.8f,1.45f,-4.6f),green,2);
 Label("REPAIR: ISOLATE > BYPASS > REBOOT",new Vector3(-4.4f,3.55f,-4.6f),.07f,Color.white);
 Panel("O2 RESERVE +35%","oxygen",new Vector3(4.4f,1.55f,-4),green,.7f);
 // Cargo bay with three independently latched recovery sockets.
 var gravity=mission.gameObject.AddComponent<GravitySystem>();gravity.Mission=mission;gravity.LooseParts=new Rigidbody[3];
 for(int i=0;i<3;i++){var g=Part("cargo","CARGO "+(i+1),new Vector3(-3+i*2,1.2f,4),new Vector3(.55f,.7f,.55f),orange);gravity.LooseParts[i]=g.GetComponent<Rigidbody>();Socket("cargo",new Vector3(-2+i*2,1.8f,8),green);}
 Cube("Recovery rack",new Vector3(0,.6f,8.4f),new Vector3(6,1.2f,.5f),dark);Label("MAGNETIC RECOVERY / 3 CANISTERS",new Vector3(0,3,8.1f),.1f,Color.green);
 // Escape craft is a small cabin with a launch console and departure animation.
 var beforePod=new System.Collections.Generic.HashSet<Transform>(Root.GetComponentsInChildren<Transform>());
 Cube("Escape pod deck",new Vector3(0,.1f,15.5f),new Vector3(4,.2f,5),white);Cube("Pod port shell",new Vector3(-2.1f,1.6f,15.5f),new Vector3(.25f,3.2f,5),white);Cube("Pod starboard shell",new Vector3(2.1f,1.6f,15.5f),new Vector3(.25f,3.2f,5),white);Cube("Pod roof",new Vector3(0,3.3f,15.5f),new Vector3(4.5f,.2f,5),dark);Panel("LAUNCH ESCAPE POD","launch",new Vector3(0,1.6f,17),green,3);
 Label("POD 07 / TWO SOULS HOME",new Vector3(0,3.75f,16.8f),.11f,Color.green);
 for(int i=0;i<2;i++){Cube("Crash seat",new Vector3(i==0?-1.1f:1.1f,.65f,15.5f),new Vector3(.8f,.6f,1),dark);Cube("Seat back",new Vector3(i==0?-1.1f:1.1f,1.2f,16),new Vector3(.8f,1.2f,.2f),orange);}
 var pod=new GameObject("Escape pod flight root");pod.transform.SetParent(Root.transform);pod.transform.position=new Vector3(0,0,15.5f);foreach(var t in Root.GetComponentsInChildren<Transform>())if(t!=pod.transform&&!beforePod.Contains(t))t.SetParent(pod.transform,true);
 // Space vista: deterministic stars, orbital rings, planetary limb.
 var earth=Sphere("Planetary limb",new Vector3(65,-31,27),new Vector3(85,85,85),Mat("Ocean",new Color(.04f,.25f,.48f),true));Object.DestroyImmediate(earth.GetComponent<Collider>());
 var random=new System.Random(904);for(int i=0;i<140;i++){var p=new Vector3(25+(float)random.NextDouble()*50,5+(float)random.NextDouble()*55,-65+(float)random.NextDouble()*140);var star=Sphere("Star",p,Vector3.one*(.04f+(float)random.NextDouble()*.12f),white);Object.DestroyImmediate(star.GetComponent<Collider>());}
 for(int i=0;i<6;i++)Cube("Exterior solar array",new Vector3(14,0,-15+i*6),new Vector3(7,.2f,3),dark);
 var rig=new GameObject("Operator / desktop + XR simulator");rig.transform.SetParent(Root.transform);rig.transform.position=new Vector3(0,.15f,-16);var cc=rig.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.28f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.25f;
 var player=rig.AddComponent<PlayerRig>();player.Mission=mission;var cam=new GameObject("Main Camera");cam.tag="MainCamera";cam.transform.SetParent(rig.transform);cam.transform.localPosition=new Vector3(0,1.6f,0);player.View=cam.AddComponent<Camera>();player.View.fieldOfView=72;player.View.nearClipPlane=.05f;player.View.farClipPlane=300;player.View.backgroundColor=new Color(.002f,.005f,.015f);cam.AddComponent<AudioListener>();
 player.LeftHand=Cube("Sim left controller",Vector3.zero,Vector3.one*.09f,cyan).transform;player.RightHand=Cube("Sim right controller",Vector3.zero,Vector3.one*.09f,orange).transform;player.LeftHand.SetParent(cam.transform);player.RightHand.SetParent(cam.transform);Object.DestroyImmediate(player.LeftHand.GetComponent<Collider>());Object.DestroyImmediate(player.RightHand.GetComponent<Collider>());
 var hud=mission.gameObject.AddComponent<MissionHUD>();hud.Mission=mission;hud.Rig=player;mission.gameObject.AddComponent<Soundscape>();var effects=mission.gameObject.AddComponent<StationEffects>();effects.Mission=mission;effects.Robot=robot;effects.Rig=player;
 mission.gameObject.AddComponent<RuntimeVerification>();
 }
}}
