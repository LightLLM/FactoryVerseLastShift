using UnityEngine;
namespace FactoryVerse {
public class Soundscape:MonoBehaviour {
 public static Soundscape Instance;AudioSource speaker,alarm,hum;MissionDirector mission;float nextAlarm;
 void Awake(){Instance=this;}
 void Start(){mission=FindAnyObjectByType<MissionDirector>();speaker=gameObject.AddComponent<AudioSource>();alarm=gameObject.AddComponent<AudioSource>();hum=gameObject.AddComponent<AudioSource>();hum.clip=Clip(55,2);hum.loop=true;hum.volume=.045f;hum.Play();}
 public void Tone(float hz,float seconds){if(speaker)speaker.PlayOneShot(Clip(hz,seconds),.18f);}
 static AudioClip Clip(float hz,float seconds){int n=(int)(22050*seconds);float[] samples=new float[n];for(int i=0;i<n;i++)samples[i]=Mathf.Sin(i*hz*Mathf.PI*2/22050)*Mathf.Min(1,i/150f)*Mathf.Min(1,(n-i)/800f);var clip=AudioClip.Create("Synth "+hz,n,1,22050,false);clip.SetData(samples,0);return clip;}
 void Update(){if(!mission||!mission.Started||mission.Terminal||mission.Paused)return;if(mission.Current>=MissionDirector.Phase.Diagnose&&Time.time>nextAlarm){nextAlarm=Time.time+4;alarm.PlayOneShot(Clip(mission.ZeroG?420:330,.5f),.09f);}}
 void OnDestroy(){if(Instance==this)Instance=null;}
}}
