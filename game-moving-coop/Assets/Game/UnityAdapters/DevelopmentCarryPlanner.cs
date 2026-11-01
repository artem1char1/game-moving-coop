using System;
using System.Collections.Generic;
using System.Linq;
using MovingCoop.Application;
using UnityEngine;
namespace MovingCoop.UnityAdapters
{
    // Development input source. Uses only published frames and normal commands, never writes world state.
    public sealed class DevelopmentCarryPlanner
    {
        private readonly bool host, sofa;
        private readonly Queue<Vector2> route = new();
        private int stage, target;
        private string command;
        private double retryAt;
        public float Turn { get; private set; }
        public DevelopmentCarryPlanner(bool host, bool sofa) { this.host=host; this.sofa=sofa; }
        public string PopCommand() { var value=command; command=null; return value; }
        private static Vector2 Point(PlayerFrame p)=>new(p.x,p.z);
        private static Vector2 Point(CargoFrame c)=>new(c.x,c.z);
        private bool Walk(Vector2 current, out Vector2 direction)
        {
            while(route.Count>0 && Vector2.Distance(current,route.Peek())<.10f)route.Dequeue();
            direction=route.Count==0?Vector2.zero:Vector2.ClampMagnitude((route.Peek()-current)*4,1);
            return route.Count==0;
        }
        private void Route(params Vector2[] points) {route.Clear();foreach(var p in points)route.Enqueue(p);}
        public Vector2 Read(SessionFrame frame)
        {
            Turn=0;
            if(frame?.phase!=1 || frame.cargo==null)return Vector2.zero;
            var p=host?frame.players.FirstOrDefault(v=>v.id==0):frame.players.FirstOrDefault(v=>v.id!=0);
            if(p==null)return Vector2.zero;
            var couch=frame.cargo.First(v=>v.id==10);
            if(sofa && !couch.delivered)return Sofa(frame,p,couch);
            if(!host)return Vector2.zero;
            if(frame.total>=320){if(stage!=99){command="evacuate";stage=99;}return Vector2.zero;}
            if(stage>=50)stage=0;
            var held=frame.cargo.FirstOrDefault(c=>c.first==(long)p.id || c.second==(long)p.id);
            Vector2 movement;
            if(stage==0)
            {
                var box=frame.cargo.FirstOrDefault(c=>c.id!=10&&!c.delivered);
                if(box==null)return Vector2.zero;
                target=box.id;float room=target<=3?-6:target<=6?0:6;
                Route(new Vector2(p.x,2),new Vector2(room,2),new Vector2(room,4.1f),new Vector2(box.x-.65f,5));stage=1;
            }
            var selected=frame.cargo.First(c=>c.id==target);
            if(stage==1){if(!Walk(Point(p),out movement))return movement;stage=2;retryAt=0;}
            if(stage==2){if(held==null){if(Time.realtimeSinceStartupAsDouble>=retryAt){command="interact";retryAt=Time.realtimeSinceStartupAsDouble+2;}return Vector2.zero;}stage=3;}
            if(stage==3)
            {
                if(held==null){stage=0;return Vector2.zero;}
                float error=Mathf.DeltaAngle(held.angle,90);
                if(Mathf.Abs(error)>.5f){Turn=Mathf.Clamp(error/5,-1,1);return Vector2.zero;}
                float room=target<=3?-6:target<=6?0:6;
                Route(new Vector2(room,held.z),new Vector2(room,1.5f),new Vector2(0,1.5f),new Vector2(0,-6));stage=4;
            }
            if(stage==4){if(held==null){stage=0;return Vector2.zero;}if(!Walk(Point(held),out movement))return movement;command="interact";stage=5;}
            if(stage==5 && selected.delivered)stage=0;
            return Vector2.zero;
        }
        private Vector2 Sofa(SessionFrame frame,PlayerFrame p,CargoFrame c)
        {
            Vector2 movement;
            if(stage==0){Route(new Vector2(0,2),new Vector2(0,5.7f),new Vector2(host?-1.4f:1.4f,7));stage=50;}
            if(stage==50){if(!Walk(Point(p),out movement))return movement;stage=51;retryAt=0;}
            bool mine=c.first==(long)p.id||c.second==(long)p.id;
            if(stage==51)
            {
                if(!mine){if((host||c.first==0)&&Time.realtimeSinceStartupAsDouble>=retryAt){command="interact";retryAt=Time.realtimeSinceStartupAsDouble+2;}return Vector2.zero;}
                if(c.first<0||c.second<0)return Vector2.zero;
                stage=52;
            }
            if(stage==52)
            {
                float error=Mathf.DeltaAngle(c.angle,90);
                if(Mathf.Abs(error)>.5f){if(host)Turn=Mathf.Clamp(error/5,-1,1);return Vector2.zero;}
                Route(new Vector2(0,-6));stage=53;
            }
            if(stage==53){if(!mine){stage=54;return Vector2.zero;}if(!Walk(Point(c),out movement))return movement;if(host)command="interact";stage=54;}
            return Vector2.zero;
        }
    }
}

