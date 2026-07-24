using System;
using System.Net;

namespace Deucarian.CommandRouting.UdpIntegration
{
    public sealed class UdpCommandTransportOptions
    {
        public UdpCommandTransportOptions(
            string bindAddress =
                UdpCommandTransportSettings.DefaultBindAddress,
            int port = UdpCommandTransportSettings.DefaultPort,
            int maximumDatagramBytes =
                UdpCommandTransportSettings
                    .DefaultMaximumDatagramBytes,
            bool sendResponses = true,
            UdpCommandMessageFormat messageFormat =
                UdpCommandMessageFormat.Json)
        {
            if (!IPAddress.TryParse(
                    bindAddress,
                    out IPAddress parsedAddress))
            {
                throw new ArgumentException(
                    "The UDP bind address is invalid.",
                    nameof(bindAddress));
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(port),
                    "The UDP port must be between 1 and 65535.");
            }

            if (maximumDatagramBytes < 256 ||
                maximumDatagramBytes >
                UdpCommandTransportSettings
                    .DefaultMaximumDatagramBytes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumDatagramBytes),
                    "The UDP datagram size is invalid.");
            }

            BindAddress = parsedAddress;
            Port = port;
            MaximumDatagramBytes = maximumDatagramBytes;
            SendResponses = sendResponses;
            MessageFormat = messageFormat;
        }

        public IPAddress BindAddress { get; }
        public int Port { get; }
        public int MaximumDatagramBytes { get; }
        public bool SendResponses { get; }
        public UdpCommandMessageFormat MessageFormat { get; }

        public static UdpCommandTransportOptions From(
            UdpCommandTransportSettings settings)
        {
            return settings == null
                ? new UdpCommandTransportOptions()
                : new UdpCommandTransportOptions(
                    settings.BindAddress,
                    settings.Port,
                    settings.MaximumDatagramBytes,
                    settings.SendResponses,
                    settings.MessageFormat);
        }
    }
}
