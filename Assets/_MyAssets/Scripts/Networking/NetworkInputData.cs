using Fusion;

namespace PhotonKarts.Networking
{
    /// <summary>
    /// Button indices for kart input transmitted over the network each tick.
    /// </summary>
    public enum KartButton
    {
        Accelerate = 0,
        Brake      = 1,
    }

    /// <summary>
    /// The input payload sent from each client to the server every network tick.
    /// Kept small — only the data that must cross the wire.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        /// <summary>Bit-packed booleans for digital inputs (accelerate, brake).</summary>
        public NetworkButtons Buttons;

        /// <summary>Analog steering value in the range [-1, 1].</summary>
        public float SteerInput;
    }
}
