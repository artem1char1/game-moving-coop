using System.Collections.Generic;
using MovingCoop.Application;
using UnityEngine;
namespace MovingCoop.UnityAdapters {
 public sealed class CargoPresentation {
 private readonly Transform parent; private readonly Dictionary<int,GameObject> views=new();
 public CargoPresentation(Transform parent){this.parent=parent;}
 public void Apply(SessionFrame frame,ulong localId){if(frame.cargo==null)return;foreach(var c in frame.cargo){if(!views.TryGetValue(c.id,out var go)){go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Cargo_"+c.id;go.transform.SetParent(parent);Object.Destroy(go.GetComponent<Collider>());go.transform.localScale=c.id==10?new Vector3(2.2f,.9f,.9f):Vector3.one*.6f;var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=c.id==10?new Color(.6f,.25f,.7f):new Color(.8f,.6f,.3f);go.GetComponent<Renderer>().material=mat;views.Add(c.id,go); for(int h=0;h<(c.id==10?2:1);h++){var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Handle_"+h;Object.Destroy(marker.GetComponent<Collider>());marker.transform.SetParent(go.transform,false);marker.transform.localPosition=new Vector3((c.id==10?(h==0?-1.4f:1.4f):-.65f)/go.transform.localScale.x,0,0);marker.transform.localScale=new Vector3(.18f/go.transform.localScale.x,.18f/go.transform.localScale.y,.18f/go.transform.localScale.z);marker.GetComponent<Renderer>().material=new Material(Shader.Find("Universal Render Pipeline/Lit"));}}foreach(Transform marker in go.transform){bool highlighted=false;foreach(var p in frame.players)if(p.id==localId&&p.targetCargo==c.id&&marker.name=="Handle_"+p.targetHandle)highlighted=true;marker.GetComponent<Renderer>().material.color=highlighted?Color.yellow:Color.white;}go.SetActive(!c.delivered);go.transform.position=new Vector3(c.x,c.id==10?.45f:.3f,c.z);go.transform.rotation=Quaternion.Euler(0,c.angle,0);}}
 public void Clear(){foreach(var go in views.Values){foreach(Transform marker in go.transform)Object.Destroy(marker.GetComponent<Renderer>().material);Object.Destroy(go.GetComponent<Renderer>().material);Object.Destroy(go);}views.Clear();}
 }
}
