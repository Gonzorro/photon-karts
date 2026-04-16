using Fusion;
using KartGame.KartSystems;
using PhotonKarts.Networking;
using UnityEngine;

namespace PhotonKarts.Kart
{
    /// <summary>
    /// Bridge between Fusion's network tick input and ArcadeKart's IInput interface.
    ///
    /// ArcadeKart.Awake() calls GetComponents&lt;IInput&gt;() and caches every IInput on the
    /// same GameObject. Each FixedUpdate it calls GenerateInput() on all of them.
    /// This component is the ONLY IInput on the networked kart prefab — it returns
    /// whatever NetworkedKartController last wrote into it for the current tick.
    ///
    /// Flow per tick:
    ///   1. NetworkedKartController.FixedUpdateNetwork() reads GetInput&lt;NetworkInputData&gt;()
    ///   2. It calls SetInput() on this proxy with the resolved data
    ///   3. ArcadeKart.FixedUpdate() calls GatherInputs() → GenerateInput() on this proxy
    ///   4. ArcadeKart drives physics with the correct tick input
    /// </summary>
    [DefaultExecutionOrder(-50)] // run before ArcadeKart reads input
    public class FusionInputProxy : MonoBehaviour, IInput
    {
        private InputData _current;

        /// <summary>
        /// Set by NetworkedKartController each FixedUpdateNetwork before ArcadeKart.FixedUpdate fires.
        /// </summary>
        public void SetInput(NetworkInputData networkInput)
        {
            _current = new InputData
            {
                Accelerate = networkInput.Accelerate,
                Brake      = networkInput.Brake,
                TurnInput  = networkInput.SteerInput,
            };
        }

        /// <summary>
        /// Called by ArcadeKart.GatherInputs() each physics tick.
        /// </summary>
        public InputData GenerateInput() => _current;
    }
}
