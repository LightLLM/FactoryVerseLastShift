using UnityEngine;
namespace FactoryVerse {
[RequireComponent(typeof(Rigidbody))]
public class Grabbable:MonoBehaviour {public string Kind,Title;public bool Held,Locked;public Vector3 Home;
 void Start(){Home=transform.position;}
 void FixedUpdate(){if(!Held&&!Locked&&(transform.position.y<-.5f||Mathf.Abs(transform.position.x)>9||Mathf.Abs(transform.position.z)>22)){transform.position=Home;GetComponent<Rigidbody>().linearVelocity=Vector3.zero;}}
}
}
