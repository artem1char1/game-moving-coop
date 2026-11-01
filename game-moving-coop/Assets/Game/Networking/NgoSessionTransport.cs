using System;
using MovingCoop.Application;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MovingCoop.Networking
{
    // NGO owns connection/delivery. This adapter only serializes the small application protocol.
    public sealed class NgoSessionTransport : IDisposable
    {
        private const string CommandChannel = "moving.commands.v1", SnapshotChannel = "moving.snapshot.v1";
        private readonly GameObject root;
        private readonly NetworkManager manager;
        private readonly UnityTransport transport;
        private bool registered, disposed;
        public event Action<ulong> Connected, Disconnected;
        public event Action<ulong, ClientCommand> CommandReceived;
        public event Action<SessionFrame> FrameReceived;
        public event Action<string> ConnectionNotice;
        public Func<bool> AllowRemoteJoin;
        public bool IsHost => manager != null && manager.IsHost;
        public bool IsListening => manager != null && manager.IsListening;
        public bool IsConnected => manager != null && manager.IsConnectedClient;
        public ulong LocalId => manager.LocalClientId;
        public NgoSessionTransport()
        {
            root = new GameObject("SessionTransport");
            transport = root.AddComponent<UnityTransport>();
            manager = root.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = false,
                TickRate = 20, ForceSamePrefabs = false
            };
            manager.ConnectionApprovalCallback += Approve;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            manager.OnTransportFailure += OnTransportFailure;
        }
        public bool StartHost(ushort port)
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            var started = manager.StartHost();
            if (started) Register();
            return started;
        }
        public bool StartClient(string address, ushort port)
        {
            transport.SetConnectionData(address, port);
            var started = manager.StartClient();
            if (started) Register();
            return started;
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var allowed = request.ClientNetworkId == NetworkManager.ServerClientId || (manager.ConnectedClientsIds.Count < 2 && (AllowRemoteJoin?.Invoke() ?? false));
            response.Approved = allowed;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = allowed ? "" : "Session full or shift already active. Join in preparation.";
            if (!allowed) ConnectionNotice?.Invoke("join_rejected");
        }
        private void Register()
        {
            manager.CustomMessagingManager.RegisterNamedMessageHandler(CommandChannel, ReceiveCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotChannel, ReceiveFrame);
            registered = true;
        }
        private void OnConnected(ulong id) => Connected?.Invoke(id);
        private void OnDisconnected(ulong id)
        {
            Disconnected?.Invoke(id);
            if (!manager.IsServer) ConnectionNotice?.Invoke(string.IsNullOrEmpty(manager.DisconnectReason) ? "Connection closed by host." : manager.DisconnectReason);
        }
        private void OnTransportFailure() => ConnectionNotice?.Invoke("Transport failed. Return to preparation.");
        public void SendCommand(ClientCommand command)
        {
            if (IsHost) { CommandReceived?.Invoke(LocalId, command); return; }
            if (!IsConnected) return;
            Send(CommandChannel, NetworkManager.ServerClientId, JsonUtility.ToJson(command), command.kind == "input" ? NetworkDelivery.UnreliableSequenced : NetworkDelivery.ReliableSequenced);
        }
        public void Publish(SessionFrame frame)
        {
            FrameReceived?.Invoke(frame);
            foreach (var id in manager.ConnectedClientsIds)
                if (id != NetworkManager.ServerClientId) Send(SnapshotChannel, id, JsonUtility.ToJson(frame), NetworkDelivery.ReliableFragmentedSequenced);
        }
        private void Send(string channel, ulong to, string json, NetworkDelivery delivery)
        {
            using var writer = new FastBufferWriter(16384, Allocator.Temp);
            writer.WriteValueSafe(json);
            manager.CustomMessagingManager.SendNamedMessage(channel, to, writer, delivery);
        }
        private void ReceiveCommand(ulong sender, FastBufferReader reader)
        {
            if (!IsHost) return;
            try { reader.ReadValueSafe(out string json); CommandReceived?.Invoke(sender, JsonUtility.FromJson<ClientCommand>(json)); }
            catch (Exception ex) { ConnectionNotice?.Invoke("Invalid command: " + ex.GetType().Name); }
        }
        private void ReceiveFrame(ulong sender, FastBufferReader reader)
        {
            if (IsHost || sender != NetworkManager.ServerClientId) return;
            try { reader.ReadValueSafe(out string json); FrameReceived?.Invoke(JsonUtility.FromJson<SessionFrame>(json)); }
            catch (Exception ex) { ConnectionNotice?.Invoke("Invalid snapshot: " + ex.GetType().Name); }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (registered && manager.CustomMessagingManager != null)
            {
                manager.CustomMessagingManager.UnregisterNamedMessageHandler(CommandChannel);
                manager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotChannel);
            }
            manager.ConnectionApprovalCallback -= Approve;
            manager.OnClientConnectedCallback -= OnConnected;
            manager.OnClientDisconnectCallback -= OnDisconnected;
            manager.OnTransportFailure -= OnTransportFailure;
            manager.Shutdown();
            UnityEngine.Object.Destroy(root);
        }
    }
}
