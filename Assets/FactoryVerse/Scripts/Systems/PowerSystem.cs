using UnityEngine;
namespace FactoryVerse {
public class PowerSystem : MonoBehaviour {
 public MissionDirector Mission;
 public bool IsRestored => Mission!=null && Mission.Current>=MissionDirector.Phase.GravityFailure && Mission.Current!=MissionDirector.Phase.Failed;
}}
