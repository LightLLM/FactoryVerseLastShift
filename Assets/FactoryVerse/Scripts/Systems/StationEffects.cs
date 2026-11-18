using UnityEngine;
namespace FactoryVerse {
public class StationEffects:MonoBehaviour {
 public MissionDirector Mission;public RobotAssembly Robot;public PlayerRig Rig;Light warning;GameObject atlas;float extraction;bool following,departing;Transform pod;GameObject[] debris=new GameObject[12];
 void Start(){var o=new GameObject("Emergency beacon");o.transform.position=new Vector3(0,3,-4);warning=o.AddComponent<Light>();warning.type=LightType.Point;warning.range=25;warning.color=Color.red;warning.intensity=0;
 for(int i=0;i<debris.Length;i++){var d=GameObject.CreatePrimitive(PrimitiveType.Cube);d.name="Orbital debris "+i;d.transform.position=new Vector3(11+(i%3)*2,1+(i%4),-18+i*3);d.transform.localScale=new Vector3(.3f,.15f,.6f);d.GetComponent<Renderer>().sharedMaterial=GameObject.Find("ISOLATE").GetComponent<Renderer>().sharedMaterial;Destroy(d.GetComponent<Collider>());d.SetActive(false);debris[i]=d;}}

 void Update(){
 if(!Mission.Started||Mission.Paused)return;
 for(int i=0;i<debris.Length;i++){var d=debris[i];d.SetActive(Mission.Current>=MissionDirector.Phase.Diagnose);if(d.activeSelf){d.transform.Rotate(new Vector3(13,23,7)*Time.deltaTime);d.transform.position+=Vector3.back*Time.deltaTime*(2+i*.12f);if(d.transform.position.z < -22)d.transform.position+=Vector3.forward*44;}}

 warning.intensity=Mission.Current>=MissionDirector.Phase.Diagnose&&!Mission.Terminal?.4f+Mathf.PingPong(Time.time*1.5f,1.5f):0;
 if(Mission.Current>=MissionDirector.Phase.Diagnose&&Mission.Current!=MissionDirector.Phase.Failed){
 if(!following){following=true;atlas=Robot.gameObject;foreach(var c in atlas.GetComponentsInChildren<Collider>())c.enabled=false;}
 Vector3 target=Rig.transform.position-Rig.transform.forward*1.7f+Rig.transform.right*1.15f;target.y=Mission.ZeroG?Mathf.Clamp(Rig.transform.position.y,0,3):0;
 atlas.transform.position=Vector3.Lerp(atlas.transform.position,target-new Vector3(-4,0,-12),Time.deltaTime*1.6f);
 }
 if(Mission.Launching&&!Mission.Terminal){if(!departing){departing=true;pod=GameObject.Find("Escape pod flight root").transform;Rig.GetComponent<CharacterController>().enabled=false;Rig.transform.SetParent(pod,true);Robot.transform.SetParent(pod,true);}pod.position+=new Vector3(5,1.4f,2)*Time.deltaTime;pod.rotation=Quaternion.Slerp(pod.rotation,Quaternion.Euler(0,20,8),Time.deltaTime*.2f);}
 if(Mission.Current==MissionDirector.Phase.Victory){extraction+=Time.deltaTime;RenderSettings.fogDensity=Mathf.Min(.12f,.012f+extraction*.01f);}
 }
}}
