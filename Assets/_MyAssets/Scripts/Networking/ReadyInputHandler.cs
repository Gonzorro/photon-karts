using UnityEngine;
using UnityEngine.InputSystem;

namespace PhotonKarts.Networking
{
    /// <summary>
    /// Detects local Space bar press and toggles ready state via RPC.
    /// Place in the scene alongside NetworkManager.
    /// </summary>
    public class ReadyInputHandler : MonoBehaviour
    {
        private void Update()
        {
            if (NetworkGameFlowManager.Instance == null) return;
            if (NetworkGameFlowManager.Instance.Phase != RacePhase.WaitingForReady) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                NetworkGameFlowManager.Instance.RPC_ToggleReady();
        }
    }
}
