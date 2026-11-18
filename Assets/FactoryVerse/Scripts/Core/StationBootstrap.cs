using UnityEngine;
namespace FactoryVerse {
public class StationBootstrap:MonoBehaviour {void Awake(){if(!FindAnyObjectByType<MissionDirector>())StationWorld.Create();}}
}
