using UnityEngine;
using UnityEngine.SceneManagement;
namespace FactoryVerse {
public class MissionDirector:MonoBehaviour {
 public enum Phase { Assemble, Diagnose, Repair, GravityFailure, Launch, Victory, Failed }
 public Phase Current {get;private set;}
 public float SecondsLeft=720,Oxygen=100;public int Mistakes {get;private set;}public int Secured {get;private set;}public int RelayStep {get;private set;}
 public bool Started,Paused,Launching;public float LaunchCountdown=6;public RobotAssembly Robot;public string Message="",FailureReason="";public float MessageUntil;
 public bool Terminal=>Current==Phase.Victory||Current==Phase.Failed;
 public bool ZeroG=>Current==Phase.GravityFailure||Current==Phase.Launch;
 public string Objective=>Current switch {
 Phase.Assemble=>"01 / ASSEMBLE ATLAS | Fit arm + cell, then boot",
 Phase.Diagnose=>"02 / ENGINEERING | Scan the damaged relay",
 Phase.Repair=>"03 / REPAIR | Isolate > Bypass > Reboot",
 Phase.GravityFailure=>"04 / ZERO-G | Recover 3 canisters to green rack",
 Phase.Launch=>"05 / ESCAPE | Board the pod and hold launch",
 Phase.Victory=>"EXTRACTION COMPLETE",_=>"SIGNAL LOST"};
 void Update(){
 if(FactoryVerse.FVInput.GetKeyDown(KeyCode.R)){Restart();return;}
 if(FactoryVerse.FVInput.GetKeyDown(KeyCode.Escape)&&Started&&!Terminal){Paused=!Paused;Time.timeScale=Paused?0:1;Cursor.lockState=Paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=Paused;}
 if(Started&&!Paused&&!Terminal)Tick(Time.deltaTime);
 }
 public void Tick(float dt){if(Terminal||!Started||Paused)return;if(Launching){LaunchCountdown-=dt;if(LaunchCountdown<=0){Current=Phase.Victory;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return;}}SecondsLeft=Mathf.Max(0,SecondsLeft-dt);Oxygen=Mathf.Max(0,Oxygen-dt*(ZeroG?.22f:Current==Phase.Repair?.11f:.055f));if(SecondsLeft<=0)Fail("Orbital decay exceeded the evacuation window.");else if(Oxygen<=0)Fail("Suit oxygen exhausted.");}
 public void Begin(){Started=true;Time.timeScale=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Notice("ATLAS: Last operator detected. Assemble me. We leave together.");}
 public void Notice(string msg){Message=msg;MessageUntil=Time.unscaledTime+7;}
 public bool Reject(string msg){Mistakes++;Notice(msg);Soundscape.Instance?.Tone(150,.12f);return false;}
 public bool TryAction(string action){
 if(!Started||Paused||Terminal)return false;
 if(action=="oxygen"){Oxygen=Mathf.Min(100,Oxygen+35);Notice("Emergency oxygen reserve connected. +35%");return true;}
 if(action=="assemble"&&Current==Phase.Assemble){if(!Robot||!Robot.Ready)return Reject("Fit the orange ARM and cyan CELL into their matching ATLAS sockets first.");Current=Phase.Diagnose;Notice("ATLAS ONLINE. Debris strike! Relay fault detected in Engineering.");}
 else if(action=="diagnose"&&Current==Phase.Diagnose){Current=Phase.Repair;Notice("Relay short found. Sequence: ISOLATE > BYPASS > REBOOT.");}
 else if(Current==Phase.Repair&&(action=="isolate"||action=="bypass"||action=="reboot")){
 string expected=RelayStep==0?"isolate":RelayStep==1?"bypass":"reboot";
 if(action!=expected){Oxygen=Mathf.Max(0,Oxygen-4);RelayStep=0;foreach(var station in FindObjectsByType<ActionStation>())if(station.Action=="isolate"||station.Action=="bypass"||station.Action=="reboot")station.Used=false;return Reject("Unsafe sequence. Oxygen -4%. Restart with ISOLATE.");}
 RelayStep++;if(RelayStep==3){Current=Phase.GravityFailure;Notice("Power restored. Gravity coupler lost! Recover all three drifting canisters.");}
 else Notice(RelayStep==1?"Relay isolated. Engage BYPASS.":"Bypass stable. Hold REBOOT.");
 }
 else if(action=="secure"&&Current==Phase.GravityFailure){Secured++;Notice("Cargo secured: "+Secured+" / 3");if(Secured==3){Current=Phase.Launch;Notice("ATLAS: Escape vector ready. Board the pod at the far end of the bay.");}}
 else if(action=="launch"&&Current==Phase.Launch&&!Launching){Launching=true;Notice("ATLAS: Clamps released. Hold tight. We are going home.");}
 else return Reject("Complete the current objective first.");
 Soundscape.Instance?.Tone(700,.15f);return true;
 }
 public void Fail(string reason){if(Terminal)return;FailureReason=reason;Current=Phase.Failed;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 public void Restart(){Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
}}
