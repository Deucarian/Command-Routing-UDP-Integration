using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.CommandRouting;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Deucarian.CommandRouting.UdpIntegration.Samples
{
    public sealed class UdpCommandHostSample :
        MonoBehaviour
    {
        [SerializeField]
        private UdpCommandTransportSettings udpSettings;

        [SerializeField]
        private CommandRoutingSettings routingSettings;

        private UdpCommandRoutingHost<
            UdpCommandHostSample> host;

        public string CurrentLabel { get; private set; } =
            "Ready";

        private void OnEnable()
        {
            host =
                new UdpCommandRoutingHost<
                    UdpCommandHostSample>(
                    this,
                    new ICommandHandler<
                        UdpCommandHostSample>[]
                    {
                        new SetLabelHandler()
                    },
                    udpSettings,
                    routingSettings);
            host.Start();
        }

        private void OnDisable()
        {
            host?.Dispose();
            host = null;
        }

        private sealed class SetLabelHandler :
            ICommandHandler<UdpCommandHostSample>
        {
            private static readonly IReadOnlyList<string>
                Names = new[] { "set_label" };

            public IReadOnlyList<string> CommandNames =>
                Names;

            public Task<CommandResult> HandleAsync(
                CommandExecutionContext<
                    UdpCommandHostSample> context,
                CancellationToken cancellationToken)
            {
                string value =
                    context.Command.Payload
                        .Value<string>("value");
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Task.FromResult(
                        CommandResult.Failure(
                            "invalid_label",
                            "A label value is required."));
                }

                context.Application.CurrentLabel =
                    value.Trim();
                return Task.FromResult(
                    CommandResult.Success(
                        new JObject
                        {
                            ["label"] =
                                context.Application
                                    .CurrentLabel
                        }));
            }
        }
    }
}
