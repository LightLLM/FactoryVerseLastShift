using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace FactoryVerse {
public class RuntimeVerification:MonoBehaviour {
 IEnumerator Start(){bool requested=Environment.GetCommandLineArgs().Contains("-fvqa");
 #if UNITY_EDITOR
 if(File.Exists("Validation/run-runtime-qa.flag")){requested=true;File.Delete("Validation/run-runtime-qa.flag");}
 #endif
 if(!requested)yield break;
 yield return null;var lines=new System.Collections.Generic.List<string>();bool success=false;
 try {
 var m=GetComponent<MissionDirector>();var rig=FindAnyObjectByType<PlayerRig>();
 Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);lines.Add("PASS: "+label);};
 m.Begin();check(!m.TryAction("launch"),"out-of-order launch rejected");check(!m.TryAction("assemble"),"incomplete robot cannot boot");
 var sockets=FindObjectsByType<AssemblySocket>();var parts=FindObjectsByType<Grabbable>();
 var arm=parts.First(x=>x.Kind=="arm");var cell=parts.First(x=>x.Kind=="cell");
 check(!sockets.First(x=>x.Kind=="arm").TryInsert(cell.transform),"wrong part rejected by typed socket");
 check(sockets.First(x=>x.Kind=="arm").TryInsert(arm.transform),"arm snaps");check(sockets.First(x=>x.Kind=="cell").TryInsert(cell.transform),"cell snaps");check(!sockets.First(x=>x.Kind=="arm").TryInsert(arm.transform),"occupied socket rejects duplicate");
 check(m.TryAction("assemble")&&m.Current==MissionDirector.Phase.Diagnose,"assembled robot boots");check(m.TryAction("diagnose"),"relay diagnosis");check(!m.TryAction("reboot")&&m.RelayStep==0,"unsafe sequence resets");check(m.TryAction("isolate")&&m.TryAction("bypass")&&m.TryAction("reboot"),"correct repair sequence");check(m.ZeroG,"gravity failure active");
 var cargo=parts.Where(x=>x.Kind=="cargo").ToArray();var rack=sockets.Where(x=>x.Kind=="cargo").ToArray();for(int i=0;i<3;i++)check(rack[i].TryInsert(cargo[i].transform),"cargo latch "+i);
 check(m.Current==MissionDirector.Phase.Launch,"all cargo unlocks escape");
 rig.Simulator=true;check(rig.LeftHand&&rig.RightHand,"two controller simulator rig exists");
 check(m.TryAction("launch")&&m.Launching,"escape burn started");m.Tick(6.1f);check(m.Current==MissionDirector.Phase.Victory,"escape victory");float time=m.SecondsLeft;m.Tick(5);check(m.SecondsLeft==time,"terminal timer frozen");
 var test=new GameObject("Failure QA").AddComponent<MissionDirector>();test.Started=true;test.Oxygen=.01f;test.Tick(1);check(test.Current==MissionDirector.Phase.Failed,"oxygen exhaustion fails mission");Destroy(test.gameObject);
 test=new GameObject("Timer QA").AddComponent<MissionDirector>();test.Started=true;test.SecondsLeft=.01f;test.Tick(1);check(test.Current==MissionDirector.Phase.Failed,"orbital deadline fails mission");Destroy(test.gameObject);
 success=true;
 }catch(Exception e){lines.Add("FAIL: "+e);}
 string path=Path.Combine(Application.persistentDataPath,"FactoryVerse-runtime-tests.txt");
 #if UNITY_EDITOR
 path="Validation/runtime-tests.txt";
 #endif
Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,lines);Debug.Log("Runtime verification success: "+success+"\n"+string.Join("\n",lines));
 #if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
 #else
 Application.Quit(success?0:1);
 #endif

 }
}}
