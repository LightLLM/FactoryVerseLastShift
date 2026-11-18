using UnityEngine;
namespace FactoryVerse {
// Input-agnostic adapter. Wire Unity XR Interaction Toolkit selectEntered
// to the ActionStation.Activate() method in the Inspector.
// Do not claim Quest hand tracking is implemented by this bridge.
public class XRInputBridge : MonoBehaviour {
 public ActionStation Target;
 public void Select(){if(Target!=null)Target.Activate();}
}}
