using System.Linq;
using MovingCoop.Application;
using UnityEngine;
namespace MovingCoop.UnityAdapters
{
    // Two ordinary input sources: distant first requester, close second requester.
    public sealed class DevelopmentHandleRacePlanner
    {
        private readonly bool host;
        private int stage;
        private string command;
        public DevelopmentHandleRacePlanner(bool host) => this.host=host;
        public string PopCommand(){var result=command;command=null;return result;}
        public Vector2 Read(SessionFrame frame)
        {
            if(frame?.phase!=1)return Vector2.zero;
            var p=frame.players.FirstOrDefault(p=>host?p.id==0:p.id!=0);
            if(p==null)return Vector2.zero;
            var current=new Vector2(p.x,p.z);
            var goal=stage==0?new Vector2(0,2):stage==1?new Vector2(0,4.1f):new Vector2(-.75f,host?3.9f:4.9f);
            if(stage<3)
            {
                if(Vector2.Distance(current,goal)>.04f)return Vector2.ClampMagnitude((goal-current)*4,1);
                stage++;return Vector2.zero;
            }
            if(stage==3)
            {
                var first=frame.players.First(v=>v.id==0);
                var second=frame.players.FirstOrDefault(v=>v.id!=0);
                bool request=host?second!=null&&Vector2.Distance(new Vector2(second.x,second.z),new Vector2(-.75f,4.9f))<.06f:first.z>4.02f;
                if(request){command="interact";stage=4;}
            }
            return Vector2.zero;
        }
    }
}
