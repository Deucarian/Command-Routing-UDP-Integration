using System;
using Newtonsoft.Json.Linq;
using Deucarian.CommandRouting;

namespace Deucarian.CommandRouting.UdpIntegration
{
    public sealed class LegacyPlainTextCommandProtocolCodec :
        ICommandProtocolCodec
    {
        public bool TryDecode(
            string message,
            out CommandEnvelope command,
            out CommandResult failure)
        {
            command = null;
            string trimmed =
                message == null
                    ? string.Empty
                    : message.Trim();
            if (trimmed.Length == 0)
            {
                failure = CommandResult.Failure(
                    CommandRoutingErrorCodes.EmptyMessage,
                    "A command message is required.");
                return false;
            }

            int separator = FindSeparator(trimmed);
            string commandName =
                separator < 0
                    ? trimmed
                    : trimmed.Substring(0, separator);
            string value =
                separator < 0
                    ? string.Empty
                    : trimmed.Substring(separator).Trim();
            var payload = new JObject();
            if (value.Length > 0)
            {
                payload["value"] = value;
            }

            command =
                new CommandEnvelope(
                    commandName,
                    payload,
                    rawEnvelope: new JObject
                    {
                        ["command"] = commandName,
                        ["payload"] = payload.DeepClone()
                    });
            failure = null;
            return true;
        }

        public string EncodeResult(
            CommandEnvelope command,
            CommandResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            string commandName =
                command?.CommandName ?? string.Empty;
            return result.Succeeded
                ? "OK " + commandName
                : "ERROR " + result.ErrorCode;
        }

        private static int FindSeparator(string value)
        {
            for (int index = 0; index < value.Length; index++)
            {
                if (char.IsWhiteSpace(value[index]))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
