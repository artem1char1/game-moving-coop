using System;
using MovingCoop.Application;
using MovingCoop.Networking;
using MovingCoop.UnityAdapters;
using UnityEngine;

namespace MovingCoop.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private NgoSessionTransport network;
        private AuthoritySession authority;
        private PlayerPresentation presentation;
        private DevelopmentScenario scenario;
        private CargoPresentation cargoPresentation;
        private GameHud hud;
        private IsometricCamera cameraRig;
        private SessionFrame frame;
        private float sendAt, snapshotAt;
        private int inputSequence, operationSequence, readyShift;
        private string readySession;
        private bool autoStarted, leaving, captured, quitting;
        private ushort Port => ushort.TryParse(DevelopmentScenario.Argument("--moving-port"),out var port)?port:(ushort)7777;
        private void Awake()
        {
            UnityEngine.Application.runInBackground = true;
            Time.fixedDeltaTime = 0.02f;
            hud = gameObject.AddComponent<GameHud>();
            presentation = new PlayerPresentation(transform);
            cameraRig = Camera.main.GetComponent<IsometricCamera>();
            scenario = new DevelopmentScenario(); cargoPresentation=new CargoPresentation(transform);
            hud.Host = () => Begin(true, "127.0.0.1");
            hud.Join = () => Begin(false, hud.Address);
            hud.StartShift = () => Send(frame?.phase==2?"restart":"start");
            hud.Leave = Leave;
        }
        private void Start()
        {
            if (scenario.Role == "host") Begin(true, "127.0.0.1");
            else if (scenario.Role == "client") Begin(false, scenario.Address);
        }
        private void Begin(bool host, string address)
        {
            if (network != null) return;
            frame = null; readySession = null; readyShift = 0; inputSequence = operationSequence = 0;
            autoStarted = false; leaving = false;
            authority = host ? new AuthoritySession(new PlayerWorld()) : null;
            network = new NgoSessionTransport();
            network.AllowRemoteJoin = () => authority != null && authority.State.CanJoin;
            network.Connected += Connected;
            network.Disconnected += Disconnected;
            network.CommandReceived += (id, command) => authority?.Enqueue(id, command);
            network.FrameReceived += Receive;
            network.ConnectionNotice += Notice;
            if (authority != null) authority.Decision += (kind, sender) => scenario.Log(kind, "sender=" + sender, network.LocalId, authority.Snapshot());
            var success = host ? network.StartHost(Port) : network.StartClient(address, Port);
            hud.Listening = success; hud.IsHost = host;
            hud.Notice = success ? (host ? "Host waiting for a second mover." : "Connecting to " + address + "...") : "Could not start transport. Check address / port.";
            if (!success) leaving = true;
        }
        private void Connected(ulong id)
        {
            if (network.IsHost) authority.Connect(id, id == 0);
            scenario.Log("connected", "player=" + id, network.LocalId, null);
            hud.Notice = network.IsHost ? "Connected. Waiting for Ready." : "Connected. Waiting for initial snapshot.";
        }
        private void Disconnected(ulong id)
        {
            if (network.IsHost)
            {
                authority.Disconnect(id);
                hud.Notice = "Client disconnected. Host may evacuate at the van, then restart.";
            }
            else leaving = true;
            scenario.Log("disconnected", "player=" + id, network.LocalId, frame);
        }
        private void Notice(string message)
        {
            hud.Notice = message;
            scenario.Log("notice", message, network.LocalId, frame);
        }
        private void Receive(SessionFrame incoming)
        {
            if (incoming?.players == null || incoming.protocol != 1) return;
            if (frame != null && incoming.session == frame.session && incoming.shift == frame.shift && incoming.revision < frame.revision) return;
            var previousPhase = frame?.phase ?? -1;
            frame = incoming;
            hud.Frame = incoming; hud.LocalId=network.LocalId;
            presentation.Apply(incoming); cargoPresentation.Apply(incoming,network.LocalId);
            cameraRig.Target = presentation.LocalTarget(network.LocalId);
            cameraRig.CargoOcclusionPoints.Clear();
            if(incoming.cargo!=null)foreach(var c in incoming.cargo)if(c.first==(long)network.LocalId||c.second==(long)network.LocalId){float a=c.angle*Mathf.Deg2Rad;for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){float dx=x*(c.id==10?1.1f:.3f),dz=z*(c.id==10?.45f:.3f);cameraRig.CargoOcclusionPoints.Add(new Vector3(c.x+Mathf.Cos(a)*dx+Mathf.Sin(a)*dz,.7f,c.z-Mathf.Sin(a)*dx+Mathf.Cos(a)*dz));}}
            if (!network.IsHost && incoming.phase == 0 && (readySession != incoming.session || readyShift != incoming.shift))
            {
                readySession = incoming.session; readyShift = incoming.shift;
                Send("ready");
                scenario.Log("initial_snapshot_ready", "", network.LocalId, frame);
            }
            hud.Notice = SnapshotNotice.Apply(previousPhase, incoming, hud.Notice);
        }
        private void Update()
        {
            if (scenario.ShouldQuit && !quitting)
            {
                quitting = true;
                scenario.Log("finished", "scenario deadline", network?.LocalId ?? 0, frame);
                UnityEngine.Application.Quit();
            }
            if (leaving) { Leave(); return; }
            presentation.Update(Time.unscaledDeltaTime, network?.IsHost ?? false);
            if (network == null) return;
            if (Time.unscaledTime >= sendAt && frame != null && network.IsConnected)
            {
                sendAt = Time.unscaledTime + 0.05f;
                var screen = scenario.Enabled ? scenario.ReadScreenInput(frame) : LocalInput.Read();
                var world = LocalInput.ToWorld(screen);
                if(!scenario.SuppressInput)Send("input", world);
            }
            if (scenario.Enabled && network.IsHost && frame?.canStart == true && !autoStarted && scenario.AutoStart)
            { autoStarted = true; Send("start"); }
            if(UnityEngine.Application.isFocused && !scenario.Enabled && UnityEngine.InputSystem.Keyboard.current != null) { var k=UnityEngine.InputSystem.Keyboard.current; if(k.eKey.wasPressedThisFrame)Send("interact"); if(k.fKey.wasPressedThisFrame)Send("evacuate"); }
            var scripted=scenario.NextCommand(); if(scripted!=null)Send(scripted);
            scenario.Observe(network.LocalId, frame, hud.Notice);
            if (!captured && !string.IsNullOrEmpty(scenario.CapturePath) && Time.realtimeSinceStartup > 12)
            { captured = true; ScreenCapture.CaptureScreenshot(scenario.CapturePath); }
        }
        private void FixedUpdate()
        {
            if (network?.IsHost != true) return;
            authority.Tick(Time.realtimeSinceStartupAsDouble, Time.fixedDeltaTime);
            if (Time.unscaledTime < snapshotAt) return;
            snapshotAt = Time.unscaledTime + 0.05f;
            network.Publish(authority.Snapshot());
        }
        private void Send(string kind, Vector2 movement = default)
        {
            if (network == null || frame == null) return;
            network.SendCommand(new ClientCommand
            {
                session = frame.session, shift = frame.shift, kind = kind,
                sequence = kind == "input" ? ++inputSequence : ++operationSequence,
                x = movement.x, z = movement.y, turn=scenario.Enabled?scenario.Turn:LocalInput.Turn()
            });
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && scenario?.Enabled != true) { Send("input"); Send("focus"); }
        }
        private void Leave()
        {
            leaving = false;
            network?.Dispose(); network = null; authority = null; frame = null;
            presentation?.Clear(); cargoPresentation?.Clear();
            if (cameraRig != null) cameraRig.Target = null;
            if (hud != null) { hud.Frame = null; hud.Listening = false; }
        }
        private void OnDestroy() { Leave(); scenario?.Dispose(); }
    }
}
