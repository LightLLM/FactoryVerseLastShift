using UnityEngine;
namespace FactoryVerse {public class OxygenSystem:MonoBehaviour {public MissionDirector Mission;public float OxygenPercent=>Mission?Mission.Oxygen:0;}}
