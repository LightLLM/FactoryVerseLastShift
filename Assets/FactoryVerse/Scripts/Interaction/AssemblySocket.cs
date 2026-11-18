using UnityEngine;
namespace FactoryVerse {
public class AssemblySocket:MonoBehaviour {
 public Transform SnapPoint;public string Kind;public MissionDirector Mission;public RobotAssembly Robot;public bool Occupied {get;private set;}
 public bool TryInsert(Transform part){
 if(Occupied||!part||!SnapPoint)return false;var item=part.GetComponent<Grabbable>();
 if(!item||item.Locked)return false;
 if(item.Kind!=Kind){Mission?.Reject("Socket mismatch. Match the component label and colour.");return false;}
 if(Kind=="cargo"&&Mission.Current!=MissionDirector.Phase.GravityFailure)return false;
 part.SetParent(SnapPoint);part.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);item.Locked=true;item.Held=false;item.GetComponent<Rigidbody>().isKinematic=true;Occupied=true;
 if(Kind=="arm")Robot.InstallArm();else if(Kind=="cell")Robot.InstallBattery();else Mission.TryAction("secure");
 Mission.Notice(Kind=="cargo"?"Cargo latched to recovery rack.":"ATLAS: "+Kind.ToUpper()+" connected.");Soundscape.Instance?.Tone(950,.12f);return true;
 }
}}
