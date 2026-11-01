using System;
namespace MovingCoop.Domain {
 public static class Heading {
 public static float FromInput(float x,float z)=>(float)(Math.Atan2(x,z)*180/Math.PI);
 public static float Towards(float current,float target,float step){float delta=(target-current)%360;if(delta>180)delta-=360;if(delta< -180)delta+=360;return current+Math.Max(-step,Math.Min(step,delta));}
 }
}
