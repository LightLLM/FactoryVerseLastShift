using UnityEngine;
namespace FactoryVerse {
public class ActionStation:MonoBehaviour {
 public MissionDirector Mission;public string Action,Title;public float HoldSeconds=1.2f;public bool Used;public float Progress;
 public void Activate(){if(!Used&&Mission!=null&&Mission.TryAction(Action))Used=true;Progress=0;}
 public void Hold(float dt){if(Used)return;Progress+=dt;if(Progress>=HoldSeconds)Activate();}
 public void Release(){Progress=0;}
}}
