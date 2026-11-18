using UnityEngine;
namespace FactoryVerse {
public class GravitySystem:MonoBehaviour {
 public MissionDirector Mission;public Rigidbody[] LooseParts;bool activated;
 void FixedUpdate(){if(!Mission||!Mission.Started||Mission.Paused||Mission.Terminal)return;
 if(!activated&&Mission.ZeroG){activated=true;for(int i=0;i<LooseParts.Length;i++){var b=LooseParts[i];if(!b)continue;b.useGravity=false;b.linearVelocity=new Vector3((i-1)*.18f,.3f,.15f);b.angularVelocity=new Vector3(.2f,.35f,.1f);}}
 if(!activated)return;foreach(var b in LooseParts){if(!b||b.isKinematic)continue;var p=b.position;if(p.y>3.3f)b.AddForce(Vector3.down*.5f);if(p.y<.7f)b.AddForce(Vector3.up*.5f);if(p.z>8)b.AddForce(Vector3.back*.4f);if(p.z<1)b.AddForce(Vector3.forward*.4f);}
 }
}}
