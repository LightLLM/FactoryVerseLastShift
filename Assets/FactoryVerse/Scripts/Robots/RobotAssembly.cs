using UnityEngine;
namespace FactoryVerse {
public class RobotAssembly : MonoBehaviour {
 public bool ArmInstalled, BatteryInstalled;
 public bool Ready=>ArmInstalled&&BatteryInstalled;
 public void InstallArm(){ArmInstalled=true;}
 public void InstallBattery(){BatteryInstalled=true;}
}}
