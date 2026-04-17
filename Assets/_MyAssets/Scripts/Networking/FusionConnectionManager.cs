using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using PhotonKarts.Kart;
using UnityEngine;
using UnityEngine.SceneManagement;
using FusionGameMode = Fusion.GameMode;

namespace PhotonKarts.Networking
{
    /// <summary>
    /// Owns the NetworkRunner lifecycle for the entire session.
    ///
    /// Server build  (#if UNITY_SERVER): starts as GameMode.Server — headless, no camera/audio/UI.
    /// Client build  (else):             starts as GameMode.AutoHostOrClient — first instance hosts,
    ///                                   rest join as clients.
    ///
    /// Also handles:
    ///   - Forwarding local player input to Fusion each tick (OnInput)
    ///   - Delegating player join/leave to KartSpawnManager (server only)
    ///   - Host migration when the dedicated server goes down
    /// </summary>
    public class FusionConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Runner")]
        [Tooltip("Prefab with NetworkRunner + NetworkSceneManagerDefault.")]
        [SerializeField] private NetworkRunner _runnerPrefab;

        [Header("Input (client only)")]
        [Tooltip("Reads keyboard/gamepad and feeds Fusion each tick. Leave null on server.")]
        [SerializeField] private FusionInputProvider _inputProvider;

        [Header("Debug HUD")]
        [SerializeField] private ConnectionStateSO _connectionState;

        // ── Constants ────────────────────────────────────────────────────────────
        private const string SessionName    = "DefaultRace";
        private const int    MaxPlayers     = 3;
        private const int    RetryCount     = 10;
        private const float  RetryDelaySec  = 2f;

        // ── Runtime ──────────────────────────────────────────────────────────────
        private NetworkRunner _runner;

        /// <summary>True when running as the authoritative server (dedicated or migrated host).</summary>
        public bool IsServer => _runner != null && _runner.IsServer;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            _connectionState?.Reset();
            SetStatus(ConnectionStatus.Connecting);

#if UNITY_SERVER
            _connectionState?.PushLog("Starting dedicated server...");
            StartServer();
#else
            _connectionState?.PushLog("Starting — looking for session...");
            ConnectAsAutoHostOrClient();
#endif
        }

        // ── Server start ─────────────────────────────────────────────────────────

        private async void StartServer()
        {
            _runner = Instantiate(_runnerPrefab);
            _runner.AddCallbacks(this);

            _connectionState?.PushLog("Runner created — StartGame (Server)...");
            var args = BuildStartArgs(FusionGameMode.Server);
            var result = await _runner.StartGame(args);

            if (result.Ok)
            {
                _connectionState?.PushLog("Session open. Waiting for players.");
                Debug.Log("[Server] Session 'DefaultRace' open. Waiting for players.");
            }
            else
            {
                _connectionState?.PushLog($"StartGame failed: {result.ShutdownReason}");
                SetStatus(ConnectionStatus.Failed);
                Debug.LogError($"[Server] StartGame failed: {result.ShutdownReason}");
            }
        }

        // ── Editor / standalone: AutoHostOrClient ────────────────────────────────

        private async void ConnectAsAutoHostOrClient()
        {
            _runner = Instantiate(_runnerPrefab);
            _runner.AddCallbacks(this);

            _connectionState?.PushLog($"Runner created — joining '{SessionName}'...");
            var args   = BuildStartArgs(FusionGameMode.AutoHostOrClient);
            var result = await _runner.StartGame(args);

            if (result.Ok)
            {
                bool isHost = _runner.IsServer;
                _connectionState?.PushLog(isHost ? "Started as HOST." : "Joined as CLIENT.");
                Debug.Log("[Client] Started as AutoHostOrClient.");
            }
            else
            {
                _connectionState?.PushLog($"StartGame failed: {result.ShutdownReason}");
                SetStatus(ConnectionStatus.Failed);
                Debug.LogError($"[Client] StartGame failed: {result.ShutdownReason}");
            }
        }

        // ── Client start with retry ───────────────────────────────────────────────

        private IEnumerator ConnectAsClientWithRetry()
        {
            for (int attempt = 1; attempt <= RetryCount; attempt++)
            {
                _runner = Instantiate(_runnerPrefab);
                _runner.AddCallbacks(this);

                bool done   = false;
                bool success = false;

                ConnectAsync(attempt, r => { success = r; done = true; });

                yield return new WaitUntil(() => done);

                if (success) yield break;

                // Runner failed — destroy it before retrying
                if (_runner != null)
                {
                    _runner.Shutdown();
                    Destroy(_runner.gameObject);
                    _runner = null;
                }

                Debug.Log($"[Client] Retry {attempt}/{RetryCount} in {RetryDelaySec}s...");
                yield return new WaitForSeconds(RetryDelaySec);
            }

            Debug.LogError("[Client] Could not connect to DefaultRace after max retries.");
        }

        private async void ConnectAsync(int attempt, Action<bool> callback)
        {
            var args   = BuildStartArgs(FusionGameMode.Client);
            var result = await _runner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log($"[Client] Joined 'DefaultRace' on attempt {attempt}.");
                callback(true);
            }
            else
            {
                Debug.LogWarning($"[Client] Attempt {attempt} failed: {result.ShutdownReason}");
                callback(false);
            }
        }

        // ── Host migration ────────────────────────────────────────────────────────

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            Debug.Log("[Client] OnHostMigration received — restarting as Host.");
            _connectionState?.PushLog("Host left — migrating to host...");
            StartCoroutine(MigrateToHost(hostMigrationToken));
        }

        private IEnumerator MigrateToHost(HostMigrationToken token)
        {
            var oldRunner = _runner;
            _runner = null;

            oldRunner.Shutdown();
            yield return new WaitUntil(() => !oldRunner.IsRunning);
            Destroy(oldRunner.gameObject);

            _connectionState?.PushLog("Old runner destroyed — spawning new runner...");

            _runner = Instantiate(_runnerPrefab);
            _runner.AddCallbacks(this);

            StartAsHost(token);
        }

        private async void StartAsHost(HostMigrationToken token)
        {
            _connectionState?.PushLog("Restarting as new host...");
            var args = BuildStartArgs(FusionGameMode.Host);
            args.HostMigrationToken  = token;
            args.HostMigrationResume = OnHostMigrationResume;

            var result = await _runner.StartGame(args);

            if (result.Ok)
            {
                _connectionState?.PushLog("Now HOST — session restored.");
                Debug.Log("[Client] Now running as Host. Session restored.");
            }
            else
            {
                _connectionState?.PushLog($"Migration failed: {result.ShutdownReason}");
                Debug.LogError($"[Client] Host migration StartGame failed: {result.ShutdownReason}");
            }
        }

        /// <summary>
        /// Called by Fusion on the new host after migration completes.
        /// Re-associates kart NetworkObjects from the token snapshot.
        /// </summary>
        private void OnHostMigrationResume(NetworkRunner runner)
        {
            Debug.Log("[Client] Race resumed after host migration.");

            var spawnManager = FindFirstObjectByType<KartSpawnManager>();
            spawnManager?.OnHostMigrationResume(runner);
        }

        // ── INetworkRunnerCallbacks ───────────────────────────────────────────────

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            int count = runner.ActivePlayers.Count();
            Debug.Log($"[{ModeLabel()}] Player {player} joined. In session: {count}");
            _connectionState?.PushLog($"Player {player} joined ({count}/{MaxPlayers}).");
            UpdateConnectionState(runner);
            if (_connectionState != null)
                _connectionState.PlayerCount = count;

            if (runner.IsServer)
            {
                var spawnManager = FindFirstObjectByType<KartSpawnManager>();
                spawnManager?.OnPlayerJoined(runner, player);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            int count = runner.ActivePlayers.Count();
            Debug.Log($"[{ModeLabel()}] Player {player} left.");
            _connectionState?.PushLog($"Player {player} left ({count}/{MaxPlayers}).");
            if (_connectionState != null)
                _connectionState.PlayerCount = count;

            if (runner.IsServer)
            {
                var spawnManager = FindFirstObjectByType<KartSpawnManager>();
                spawnManager?.OnPlayerLeft(runner, player);
            }
        }

        /// <summary>
        /// Called each tick on the client that owns input authority.
        /// Passes local player input to Fusion for transmission and local prediction.
        /// </summary>
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
#if !UNITY_SERVER
            if (_inputProvider != null)
                input.Set(_inputProvider.GetNetworkInput());
#endif
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[{ModeLabel()}] Runner shutdown: {shutdownReason}");
            _connectionState?.PushLog($"Shutdown: {shutdownReason}");
            SetStatus(ConnectionStatus.Disconnected);
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log($"[Client] Connected to server.");
            _connectionState?.PushLog("Connected to server.");
            UpdateConnectionState(runner);
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"[Client] Disconnected from server: {reason}");
            _connectionState?.PushLog($"Disconnected: {reason}");
            SetStatus(ConnectionStatus.Disconnected);
        }

        // ── Required interface stubs ──────────────────────────────────────────────

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private StartGameArgs BuildStartArgs(FusionGameMode mode)
        {
            return new StartGameArgs
            {
                GameMode    = mode,
                SessionName = SessionName,
                PlayerCount = MaxPlayers,
                IsVisible   = false,   // not listed in public matchmaking
                IsOpen      = true,
                Scene       = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
            };
        }

        private string ModeLabel() => IsServer ? "Server" : "Client";

        private void SetStatus(ConnectionStatus status)
        {
            if (_connectionState == null) return;
            _connectionState.Status = status;
        }

        private void UpdateConnectionState(NetworkRunner runner)
        {
            if (_connectionState == null) return;
            _connectionState.Status      = ConnectionStatus.Connected;
            _connectionState.RoomName    = runner.SessionInfo.Name ?? SessionName;
            _connectionState.IsHost      = runner.IsServer;
            _connectionState.MaxPlayers  = MaxPlayers;
            _connectionState.LocalPlayer = runner.LocalPlayer.ToString();
        }
    }
}
