using System;
using System.IO;
using MovingCoop.Application;
using UnityEngine;

namespace MovingCoop.UnityAdapters
{
    // Startup harness replaces only the input source; it uses the same transport and authority.
    public sealed class DevelopmentScenario : IDisposable
    {
        [Serializable]
        private sealed class Record
        {
            public string kind, detail;
            public double time;
            public ulong local;
            public SessionFrame state;
        }
        [Serializable] private sealed class Script { public Step[] steps; }
        [Serializable] private sealed class Step { public float at, x,z,turn; public string command; }
        private DevelopmentHandleRacePlanner race;
        private DevelopmentCarryPlanner planner; private string mode, forcedCommand; private bool interrupted; public bool SuppressInput {get;private set;}
        private Step[] steps; private int commandIndex; public float Turn {get;private set;}
        public string NextCommand() { if(race!=null)return race.PopCommand(); if(forcedCommand!=null){var value=forcedCommand;forcedCommand=null;return value;} if(planner!=null)return planner.PopCommand(); if(steps==null || activeAt<0)return null; while(commandIndex<steps.Length && Time.realtimeSinceStartupAsDouble-activeAt>=steps[commandIndex].at) {var c=steps[commandIndex++].command;if(!string.IsNullOrEmpty(c))return c;} return null; }
        private StreamWriter writer;
        private double activeAt = -1, nextLog;
        private readonly double beganAt;
        public string Role { get; }
        public string Address { get; }
        public bool Enabled { get; }
        public bool AutoStart { get; }
        public bool ShouldQuit => Enabled && Time.realtimeSinceStartupAsDouble - beganAt >= duration;
        private readonly float duration;
        public string CapturePath { get; }
        public DevelopmentScenario()
        {
            Role = Argument("--moving-role");
            Address = Argument("--moving-address") ?? "127.0.0.1";
            mode=Argument("--moving-scenario");
            if(mode=="race")race=new DevelopmentHandleRacePlanner(Role=="host");
            Enabled = mode == "race" || mode == "movement" || mode=="quota" || mode!=null&&mode.StartsWith("sofa") || Argument("--moving-script")!=null;
            if(mode=="quota"||mode!=null&&mode.StartsWith("sofa"))planner=new DevelopmentCarryPlanner(Role=="host",mode!=null&&mode.StartsWith("sofa"));
            var scriptPath=Argument("--moving-script"); if(scriptPath!=null)steps=JsonUtility.FromJson<Script>(File.ReadAllText(scriptPath)).steps;
            AutoStart = Argument("--moving-autostart") != "false";
            duration = float.TryParse(Argument("--moving-seconds"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds) ? seconds : 25;
            beganAt = Time.realtimeSinceStartupAsDouble;
            CapturePath = Argument("--moving-capture");
            var path = Argument("--moving-evidence");
            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            }
            Log("startup", Role ?? "interactive", 0, null);
        }
        public static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return null;
        }
        public Vector2 ReadScreenInput(SessionFrame frame)
        {
            if (frame == null || frame.phase != 1) return Vector2.zero;
            if(race!=null){var world=race.Read(frame);return new Vector2((world.x-world.y)*.70710678f,(world.x+world.y)*.70710678f);}
            if(planner!=null){if(Role=="client"&&!interrupted&&frame.cargo!=null){foreach(var c in frame.cargo)if(c.id==10&&c.first>=0&&c.second>=0&&c.z<4){if(mode=="sofa-focus"){forcedCommand="focus";interrupted=true;}if(mode=="sofa-stale"){SuppressInput=true;interrupted=true;}}} var world=planner.Read(frame);Turn=planner.Turn;return new Vector2((world.x-world.y)*.70710678f,(world.x+world.y)*.70710678f);}
            if (activeAt < 0) activeAt = Time.realtimeSinceStartupAsDouble;
            var time = Time.realtimeSinceStartupAsDouble - activeAt;
            if(steps!=null){ Step current=null; foreach(var step in steps)if(time>=step.at)current=step; Turn=current?.turn??0;return current==null?Vector2.zero:new Vector2((current.x-current.z)*.70710678f,(current.x+current.z)*.70710678f); }
            // Same screen-to-world mapper as WASD. Walk through centre corridor into back wall, then return.
            if (time >= 1 && time < 7) return new Vector2(-0.70710678f, 0.70710678f);
            if (time >= 8 && time < 10) return new Vector2(0.70710678f, -0.70710678f);
            return Vector2.zero;
        }
        public void Observe(ulong local, SessionFrame frame, string notice)
        {
            if (Time.realtimeSinceStartupAsDouble < nextLog) return;
            nextLog = Time.realtimeSinceStartupAsDouble + 0.5;
            Log("state", notice, local, frame);
        }
        public void Log(string kind, string detail, ulong local, SessionFrame frame)
        {
            var line = JsonUtility.ToJson(new Record { kind = kind, detail = detail, time = Time.realtimeSinceStartupAsDouble, local = local, state = frame });
            writer?.WriteLine(line);
            if (kind != "state") Debug.Log("MOVING_EVIDENCE " + line);
        }
        public void Dispose() { writer?.Dispose(); writer = null; }
    }
}
