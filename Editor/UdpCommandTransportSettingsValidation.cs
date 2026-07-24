using System.Net;

namespace Deucarian.CommandRouting.UdpIntegration.Editor
{
    internal static class
        UdpCommandTransportSettingsValidation
    {
        public static string Validate(
            UdpCommandTransportSettings settings)
        {
            if (settings == null)
            {
                return "Create a UdpCommandTransportSettings asset.";
            }

            if (!IPAddress.TryParse(
                    settings.BindAddress,
                    out _))
            {
                return "The bind address must be a valid IP address.";
            }

            if (settings.Port < 1 ||
                settings.Port > 65535)
            {
                return "The UDP port must be between 1 and 65535.";
            }

            if (settings.MaximumDatagramBytes < 256 ||
                settings.MaximumDatagramBytes >
                UdpCommandTransportSettings
                    .DefaultMaximumDatagramBytes)
            {
                return "The maximum datagram size is invalid.";
            }

            return string.Empty;
        }
    }
}
