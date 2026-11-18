using UnityEngine;
namespace FactoryVerse {
public class PlayerRig:MonoBehaviour {
 public MissionDirector Mission;public Camera View;public bool Simulator;public Grabbable Held;public string Prompt="";public float Progress;
 Vector2[] offsets=new Vector2[2];float[] reaches={1.8f,1.8f};
 public Transform LeftHand,RightHand;public int ActiveHand=1;public float HandDistance=1.8f;public Vector2 HandOffset;public bool Comfortable=true;
 CharacterController motor;float yaw,pitch;Vector3 velocity;ActionStation lastStation;LineRenderer rayLine;
 void Start(){motor=GetComponent<CharacterController>();yaw=transform.eulerAngles.y;rayLine=RightHand.gameObject.AddComponent<LineRenderer>();var rayTemplate=Resources.Load<Material>("FactoryVerseRay");rayLine.material=rayTemplate?new Material(rayTemplate):new Material(Shader.Find("Sprites/Default"));rayLine.startWidth=.012f;rayLine.endWidth=.003f;rayLine.positionCount=2;rayLine.startColor=Color.cyan;rayLine.endColor=new Color(0,1,1,.2f);}
 void Update(){
 if(!Mission.Started||Mission.Paused||Mission.Terminal){rayLine.enabled=false;return;}
 if(FactoryVerse.FVInput.GetKeyDown(KeyCode.F2)){Simulator=!Simulator;Mission.Notice(Simulator?"XR SIMULATOR: Tab swaps hand. Hold middle mouse + mouse to offset hand. Wheel changes reach.":"DESKTOP: aim with centre reticle. E grabs / drops. Hold mouse to operate.");}
 if(FactoryVerse.FVInput.GetKeyDown(KeyCode.C)){Comfortable=!Comfortable;Mission.Notice(Comfortable?"Comfort mode: capped speed, no camera roll.":"Free thrust mode: increased drift speed.");}
 if(Mission.Launching){Prompt="ESCAPE BURN  /  "+Mathf.CeilToInt(Mission.LaunchCountdown);rayLine.enabled=false;return;}
 if(FactoryVerse.FVInput.GetKeyDown(KeyCode.Tab)){offsets[ActiveHand]=HandOffset;reaches[ActiveHand]=HandDistance;ActiveHand=1-ActiveHand;HandOffset=offsets[ActiveHand];HandDistance=reaches[ActiveHand];}
 bool adjusting=Simulator&&FactoryVerse.FVInput.GetMouseButton(2);
 if(adjusting){HandOffset+=new Vector2(FactoryVerse.FVInput.GetAxis("Mouse X"),FactoryVerse.FVInput.GetAxis("Mouse Y"))*.025f;HandOffset=Vector2.ClampMagnitude(HandOffset,1.2f);}
 else {yaw+=FactoryVerse.FVInput.GetAxis("Mouse X")*2;pitch=Mathf.Clamp(pitch-FactoryVerse.FVInput.GetAxis("Mouse Y")*2,-80,80);}
 transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
 if(Simulator)HandDistance=Mathf.Clamp(HandDistance+FactoryVerse.FVInput.mouseScrollDelta.y*.15f,.5f,3.2f);
 Vector3 localMove=new Vector3(FactoryVerse.FVInput.GetAxisRaw("Horizontal"),0,FactoryVerse.FVInput.GetAxisRaw("Vertical"));
 Vector3 wish=transform.TransformDirection(Vector3.ClampMagnitude(localMove,1));
 if(Mission.ZeroG){wish=View.transform.TransformDirection(Vector3.ClampMagnitude(localMove,1));wish+=Vector3.up*((FactoryVerse.FVInput.GetKey(KeyCode.Space)?1:0)-(FactoryVerse.FVInput.GetKey(KeyCode.LeftControl)?1:0));velocity=Vector3.MoveTowards(velocity,Vector3.ClampMagnitude(wish,1)*(Comfortable?2.7f:4.5f),Time.deltaTime*3);if(FactoryVerse.FVInput.GetKey(KeyCode.X))velocity=Vector3.MoveTowards(velocity,Vector3.zero,Time.deltaTime*12);}
 else {velocity.x=wish.x*3.8f;velocity.z=wish.z*3.8f;velocity.y=motor.isGrounded?-1:velocity.y-9.8f*Time.deltaTime;}
 motor.Move(velocity*Time.deltaTime);
 LeftHand.localPosition=new Vector3(-.27f,-.22f,.6f);RightHand.localPosition=new Vector3(.27f,-.22f,.6f);
 var hand=ActiveHand==0?LeftHand:RightHand;
 if(Simulator)hand.localPosition=new Vector3((ActiveHand==0?-.27f:.27f)+HandOffset.x,-.22f+HandOffset.y,HandDistance);
 var origin=Simulator?hand.position:View.transform.position;var direction=View.transform.forward;
 rayLine.enabled=Simulator;rayLine.SetPosition(0,origin);rayLine.SetPosition(1,origin+direction*3.5f);
 Prompt="";Progress=0;RaycastHit hit;ActionStation station=null;
 if(Held){
 Held.transform.position=Simulator?hand.position:View.transform.position+View.transform.forward*1.7f;Held.transform.rotation=View.transform.rotation;
 AssemblySocket nearest=null;float best=1.25f;foreach(var socket in FindObjectsByType<AssemblySocket>()){if(socket.Occupied)continue;float d=Vector3.Distance(Held.transform.position,socket.transform.position);if(d<best){best=d;nearest=socket;}}
 Prompt=nearest?"E / GRIP  •  fit "+Held.Title+" into "+nearest.Kind.ToUpper():"E / GRIP  •  release "+Held.Title+" | move close to a matching socket";
 if(FactoryVerse.FVInput.GetKeyDown(KeyCode.E)||FactoryVerse.FVInput.GetMouseButtonDown(1)){
 if(nearest&&nearest.TryInsert(Held.transform))Held=null;
 else {Held.Held=false;var rb=Held.GetComponent<Rigidbody>();rb.isKinematic=false;rb.useGravity=!Mission.ZeroG;rb.linearVelocity=Mission.ZeroG?direction*.25f:Vector3.zero;Held=null;}
 }
 }else if(Physics.Raycast(origin,direction,out hit,Simulator?4.5f:3.5f)){
 var item=hit.collider.GetComponentInParent<Grabbable>();station=hit.collider.GetComponentInParent<ActionStation>();
 if(item&&!item.Locked){Prompt="E / GRIP  •  grab "+item.Title;if(FactoryVerse.FVInput.GetKeyDown(KeyCode.E)||FactoryVerse.FVInput.GetMouseButtonDown(1)){Held=item;Held.Held=true;Held.GetComponent<Rigidbody>().isKinematic=true;}}
 else if(station&&!station.Used){Prompt="HOLD LMB / TRIGGER  •  "+station.Title;if(FactoryVerse.FVInput.GetMouseButton(0)||FactoryVerse.FVInput.GetKey(KeyCode.F))station.Hold(Time.deltaTime);else station.Release();Progress=station.Progress/station.HoldSeconds;}
 else if(station&&station.Used)Prompt="SYSTEM COMPLETE";
 }
 if(lastStation&&lastStation!=station)lastStation.Release();lastStation=station;
 }
}}
