using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using PhotonKarts.Networking;

namespace PhotonKarts.Kart
{
    /// <summary>
    /// Reads player input from the New Input System each frame and makes it available
    /// to the Fusion network runner via the OnInput callback (registered in FusionConnectionManager).
    ///
    /// This component sits on the player's local GameObject. It does NOT directly drive
    /// ArcadeKart — that is FusionInputProxy's job. This component is only concerned with
    /// gathering and exposing raw input for the network layer.
    /// </summary>
    public class FusionInputProvider : MonoBehaviour
    {
        // Cached input state written every Update, consumed once per network tick.
        private bool _accelerate;
        private bool _brake;
        private float _steer;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Arrow keys / WASD
            _accelerate = keyboard.upArrowKey.isPressed   || keyboard.wKey.isPressed;
            _brake      = keyboard.downArrowKey.isPressed  || keyboard.sKey.isPressed;
            _steer      = (keyboard.leftArrowKey.isPressed  || keyboard.aKey.isPressed ? -1f : 0f)
                        + (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ?  1f : 0f);

            // Gamepad support
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                _accelerate |= gamepad.rightTrigger.isPressed;
                _brake      |= gamepad.leftTrigger.isPressed;
                float padSteer = gamepad.leftStick.x.ReadValue();
                if (Mathf.Abs(padSteer) > 0.1f)
                    _steer = padSteer;
            }
        }

        /// <summary>
        /// Called by FusionConnectionManager.OnInput each network tick to populate the
        /// input struct that will be sent to the server and used for local prediction.
        /// </summary>
        public NetworkInputData GetNetworkInput()
        {
            var data = new NetworkInputData();
            data.Buttons.Set(KartButton.Accelerate, _accelerate);
            data.Buttons.Set(KartButton.Brake,      _brake);
            data.SteerInput = Mathf.Clamp(_steer, -1f, 1f);
            return data;
        }
    }
}
