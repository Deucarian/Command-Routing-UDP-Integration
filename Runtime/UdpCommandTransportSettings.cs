using UnityEngine;

namespace Deucarian.CommandRouting.UdpIntegration
{
    public sealed class UdpCommandTransportSettings :
        ScriptableObject
    {
        public const string DefaultBindAddress = "0.0.0.0";
        public const int DefaultPort = 9050;
        public const int DefaultMaximumDatagramBytes = 65507;

        [SerializeField]
        private string bindAddress = DefaultBindAddress;

        [SerializeField, Range(1, 65535)]
        private int port = DefaultPort;

        [SerializeField, Range(256, DefaultMaximumDatagramBytes)]
        private int maximumDatagramBytes =
            DefaultMaximumDatagramBytes;

        [SerializeField]
        private bool sendResponses = true;

        [SerializeField]
        private UdpCommandMessageFormat messageFormat =
            UdpCommandMessageFormat.Json;

        public string BindAddress =>
            string.IsNullOrWhiteSpace(bindAddress)
                ? DefaultBindAddress
                : bindAddress.Trim();

        public int Port => Mathf.Clamp(port, 1, 65535);

        public int MaximumDatagramBytes =>
            Mathf.Clamp(
                maximumDatagramBytes,
                256,
                DefaultMaximumDatagramBytes);

        public bool SendResponses => sendResponses;

        public UdpCommandMessageFormat MessageFormat =>
            messageFormat;
    }
}
