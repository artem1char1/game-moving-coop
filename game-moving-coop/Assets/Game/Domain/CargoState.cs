using System;
using System.Collections.Generic;
namespace MovingCoop.Domain {
 public sealed class CargoState {
 public int Id {get;} public bool Sofa=>Id==10; public int Value=>Sofa?200:40;
 public GroundPoint Position {get;private set;} public float Angle {get;private set;} public bool Delivered {get;private set;}
 private readonly Dictionary<int,ulong> carriers=new(); public IReadOnlyDictionary<int,ulong> Carriers=>carriers;
 public ulong Leader {get;private set;} public double InsideSince=-1;
 public CargoState(int id,GroundPoint at){Id=id;Position=at;}
 public GroundPoint Handle(int h,GroundPoint at,float angle){float d=Sofa?(h==0?-1.4f:1.4f):-.65f;double a=angle*Math.PI/180;return new GroundPoint(at.X+(float)Math.Cos(a)*d,at.Z-(float)Math.Sin(a)*d);}
 public bool Grab(int h,ulong id){if(Delivered||carriers.ContainsKey(h)||carriers.ContainsValue(id))return false;if(carriers.Count==0)Leader=id;carriers.Add(h,id);InsideSince=-1;return true;}
 public void Release(){carriers.Clear();InsideSince=-1;}
 public void Move(GroundPoint at,float angle){Position=at;Angle=angle;}
 public bool Deliver(HashSet<int> ledger){if(Delivered||carriers.Count!=0||!ledger.Add(Id))return false;Delivered=true;return true;}
 public bool FullyInside(){for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){double a=Angle*Math.PI/180;float dx=x*(Sofa?1.1f:.3f),dz=z*(Sofa?.45f:.3f);float px=Position.X+(float)(Math.Cos(a)*dx+Math.Sin(a)*dz),pz=Position.Z+(float)(-Math.Sin(a)*dx+Math.Cos(a)*dz);if(px< -2||px>2||pz< -7.5f||pz> -4.5f)return false;}return true;}
 }
}
