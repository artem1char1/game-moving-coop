using System;
using System.Collections.Generic;
using System.Linq;
using MovingCoop.Domain;
namespace MovingCoop.Application {
 public sealed class CargoSimulation {
 private readonly ICargoWorld world; public List<CargoState> Items {get;}=new(); private readonly HashSet<int> ledger=new();
 private readonly Dictionary<ulong,(int cargo,int handle)> approach=new();
 public int Total {get;private set;} public string Feedback {get;private set;}="";
 public CargoSimulation(ICargoWorld world){this.world=world;Reset();}
 public void Reset(){Items.Clear();ledger.Clear();approach.Clear();Total=0;for(int i=0;i<9;i++)Items.Add(new CargoState(i+1,new GroundPoint(-7+(i/3)*6+(i%3)*.9f,5)));Items.Add(new CargoState(10,new GroundPoint(0,7)));}
 public CargoState Held(ulong id)=>Items.FirstOrDefault(c=>c.Carriers.Values.Contains(id));
 public bool Approaching(ulong id)=>approach.ContainsKey(id);
 private bool Reserved(int cargo,int handle)=>approach.Values.Any(v=>v.cargo==cargo&&v.handle==handle);
 public void Cancel(ulong id){approach.Remove(id);var held=Held(id);if(held==null)return;foreach(var pending in approach.Where(v=>v.Value.cargo==held.Id).Select(v=>v.Key).ToArray())approach.Remove(pending);held.Release();}
 public (int cargo,int handle) Target(SessionState state,ulong id) { var p=state.Find(id); if(p==null||Held(id)!=null)return (0,0); var choices=Items.Where(c=>!c.Delivered).SelectMany(c=>Enumerable.Range(0,c.Sofa?2:1).Where(h=>!c.Carriers.ContainsKey(h)&&!Reserved(c.Id,h)).Select(h=>(c,h,at:c.Handle(h,c.Position,c.Angle)))).Where(t=>Distance(p.Position,t.at)<=1.2f && world.Clear(p.Position,t.at)).OrderBy(t=>Distance(p.Position,t.at)).ThenBy(t=>t.c.Id).ThenBy(t=>t.h); foreach(var t in choices)return (t.c.Id,t.h);return (0,0); }
 public void Interact(SessionState state,ulong id){if(Held(id)!=null||Approaching(id)){Cancel(id);return;}var p=state.Find(id);if(p==null)return;
 var targets=Items.Where(c=>!c.Delivered).SelectMany(c=>Enumerable.Range(0,c.Sofa?2:1).Where(h=>!c.Carriers.ContainsKey(h)).Select(h=>(c,h,at:c.Handle(h,c.Position,c.Angle)))).OrderBy(t=>Distance(p.Position,t.at)).ThenBy(t=>t.c.Id).ThenBy(t=>t.h);
 foreach(var t in targets){if(Distance(p.Position,t.at)>1.2f)break;if(!world.Clear(p.Position,t.at))continue;if(Reserved(t.c.Id,t.h)){Feedback="Occupied";return;}approach[id]=(t.c.Id,t.h);Feedback="Approaching handle";return;}Feedback="No reachable free handle";}
 public void Step(SessionState state,float dt,double now,Func<ulong,(float x,float z,float turn)> input){
 foreach(var pair in approach.ToArray()){var p=state.Find(pair.Key);var c=Items.First(v=>v.Id==pair.Value.cargo);if(p==null||c.Delivered||c.Carriers.ContainsKey(pair.Value.handle)){approach.Remove(pair.Key);continue;}var target=c.Handle(pair.Value.handle,c.Position,c.Angle);if(!world.Clear(p.Position,target)){approach.Remove(pair.Key);continue;}float d=Distance(p.Position,target);if(d<=.01f){bool grabbed=c.Grab(pair.Value.handle,p.Id);approach.Remove(p.Id);Feedback=!grabbed?"Occupied":c.Sofa&&c.Carriers.Count<2?"Need second mover":"Holding";}else{float step=Math.Min(4*dt,d);state.Move(p.Id,world.Move(p.Position,new GroundPoint((target.X-p.Position.X)/d*step,(target.Z-p.Position.Z)/d*step)));}}
 foreach(var c in Items.Where(v=>!v.Delivered)){if(c.Carriers.Count==(c.Sofa?2:1)){float x=0,z=0;foreach(var id in c.Carriers.Values){var v=input(id);x+=v.x;z+=v.z;}x/=c.Carriers.Count;z/=c.Carriers.Count;float angle=c.Angle+input(c.Leader).turn*90*dt;var at=new GroundPoint(c.Position.X+x*(c.Sofa?2:3)*dt,c.Position.Z+z*(c.Sofa?2:3)*dt);if(!TryPose(state,c,at,angle)){if(!TryPose(state,c,new GroundPoint(at.X,c.Position.Z),c.Angle))TryPose(state,c,new GroundPoint(c.Position.X,at.Z),c.Angle);TryPose(state,c,c.Position,angle);}}
 if(c.Carriers.Count!=0||approach.Values.Any(v=>v.cargo==c.Id)||!c.FullyInside())c.InsideSince=-1;else if(c.InsideSince<0)c.InsideSince=now;else if(now-c.InsideSince>=.5&&c.Deliver(ledger))Total+=c.Value;
 }}
 private bool TryPose(SessionState state,CargoState c,GroundPoint at,float angle){bool fits=world.CargoFits(c,at,angle);foreach(var h in c.Carriers)fits &=world.Clear(state.Find(h.Value).Position,c.Handle(h.Key,at,angle));if(!fits)return false;c.Move(at,angle);foreach(var h in c.Carriers)state.Move(h.Value,c.Handle(h.Key,at,angle));return true;}
 private static float Distance(GroundPoint a,GroundPoint b)=>(float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
 }
}
