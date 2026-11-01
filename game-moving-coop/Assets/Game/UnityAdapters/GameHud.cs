using MovingCoop.Application;
using UnityEngine;
namespace MovingCoop.UnityAdapters
{
    public sealed class GameHud : MonoBehaviour
    {
        public SessionFrame Frame; public ulong LocalId;
        public string Notice = "Start a host or join a local/LAN address.";
        public bool Listening, IsHost;
        public System.Action Host, Join, StartShift, Leave;
        private string address = "127.0.0.1";
        public string Address => address;
        private GUIStyle title, text, button;
        private void OnGUI()
        {
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            text ??= new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.85f, 0.92f, 0.95f) } };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 18, fixedHeight = 36 };
            GUILayout.BeginArea(new Rect(20, 20, 405, 490), GUI.skin.box);
            GUILayout.Label("MOVING CREW  /  prototype", title);
            GUILayout.Label("Host-authoritative co-op prototype", text);
            GUILayout.Space(8);
            GUILayout.Label(Notice, text);
            if(Frame!=null){GUILayout.Label($"{Frame.total}/320  |  {Frame.remaining:F1}s",text);GUILayout.Label(Frame.result,text); if(Frame.phase==1&&IsHost)GUILayout.Label(Frame.total<320?"F: Leave - quota not met":"F: Evacuate when all movers are free in delivery zone",text);GUILayout.Label(Frame.feedback,text); if(Frame.players!=null)foreach(var p in Frame.players)if(p.id==LocalId)GUILayout.Label(p.targetCargo>0?$"E: Cargo {p.targetCargo} / Handle {p.targetHandle}":"E: release / approach a free handle",text);if(Frame.cargo!=null)foreach(var c in Frame.cargo)if(c.id==10)GUILayout.Label($"Sofa: {c.first} / {c.second}",text);foreach(var p in Frame.players)if(p.rotationLeader)GUILayout.Label($"Rotation leader: {p.id}",text); }
            if (!Listening)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("HOST | localhost / LAN", button)) Host?.Invoke();
                address = GUILayout.TextField(address, GUILayout.Height(28));
                if (GUILayout.Button("JOIN", button)) Join?.Invoke();
            }
            else
            {
                if (Frame?.players != null)
                    foreach (var p in Frame.players) GUILayout.Label((p.id == 0 ? "BLUE / HOST" : "ORANGE / CLIENT") + (p.ready ? "  READY" : "  syncing..."), text);
                if (Frame != null && (Frame.phase == 0 || Frame.phase==2))
                {
                    GUI.enabled = IsHost && (Frame.canStart || Frame.phase==2);
                    if (GUILayout.Button(Frame.phase==2?"RESTART":"START SHIFT", button)) StartShift?.Invoke();
                    GUI.enabled = true;
                }
                if (GUILayout.Button("LEAVE SESSION", button)) Leave?.Invoke();
            }
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(20, Screen.height - 75, Screen.width - 40, 55), GUI.skin.box);
            GUILayout.Label("WASD - move relative to screen | Walls hide locally; collisions remain", text);
            GUILayout.Label("E - grab/release | Q/R - rotate | F - evacuate (host)", text);
            GUILayout.EndArea();
        }
    }
}
