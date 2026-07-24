using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.CommandRouting;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Deucarian.CommandRouting.UdpIntegration.Tests
{
    public sealed class UdpCommandIntegrationTests
    {
        [Test]
        public void Options_RejectInvalidNetworkValues()
        {
            Assert.Throws<ArgumentException>(
                () => new UdpCommandTransportOptions(
                    "not-an-ip"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UdpCommandTransportOptions(
                    port: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new UdpCommandTransportOptions(
                    maximumDatagramBytes: 100));
        }

        [Test]
        public void LegacyCodec_MapsCommandAndValue()
        {
            var codec =
                new LegacyPlainTextCommandProtocolCodec();

            bool decoded =
                codec.TryDecode(
                    "StartLogging session-42",
                    out CommandEnvelope command,
                    out CommandResult failure);

            Assert.That(decoded, Is.True);
            Assert.That(failure, Is.Null);
            Assert.That(
                command.CommandName,
                Is.EqualTo("StartLogging"));
            Assert.That(
                command.Payload.Value<string>("value"),
                Is.EqualTo("session-42"));
            Assert.That(
                codec.EncodeResult(
                    command,
                    CommandResult.Success()),
                Is.EqualTo("OK StartLogging"));
        }

        [Test]
        public void LegacyCodec_DoesNotEchoFailureDetails()
        {
            var codec =
                new LegacyPlainTextCommandProtocolCodec();
            var command =
                new CommandEnvelope("authenticate");

            string response =
                codec.EncodeResult(
                    command,
                    CommandResult.Failure(
                        "invalid_token",
                        "The token secret-value is invalid.",
                        new JObject
                        {
                            ["access_token"] =
                                "secret-value"
                        }));

            Assert.That(
                response,
                Is.EqualTo("ERROR invalid_token"));
            Assert.That(
                response,
                Does.Not.Contain("secret-value"));
        }

        [Test]
        public async Task UdpHost_RoutesJsonAndReturnsResponse()
        {
            int port = ReserveUdpPort();
            var metadata =
                new TaskCompletionSource<CommandMetadata>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);
            var handler = new PingHandler(metadata);
            var options =
                new UdpCommandTransportOptions(
                    IPAddress.Loopback.ToString(),
                    port);

            using (var host =
                   new UdpCommandRoutingHost<object>(
                       new object(),
                       new[] { handler },
                       options,
                       dispatchContext:
                           new SynchronizationContext()))
            using (var sender = new UdpClient())
            {
                host.Start();
                byte[] message =
                    Encoding.UTF8.GetBytes(
                        "{\"protocol_version\":1," +
                        "\"command_id\":\"udp-test\"," +
                        "\"command\":\"ping\"," +
                        "\"payload\":{}}");
                await sender.SendAsync(
                    message,
                    message.Length,
                    new IPEndPoint(
                        IPAddress.Loopback,
                        port));

                UdpReceiveResult received =
                    await WithTimeout(sender.ReceiveAsync());
                CommandMetadata receivedMetadata =
                    await WithTimeout(metadata.Task);
                string response =
                    Encoding.UTF8.GetString(
                        received.Buffer);

                Assert.That(response, Does.Contain("\"success\":true"));
                Assert.That(response, Does.Contain("\"pong\":true"));
                Assert.That(response, Does.Contain("udp-test"));
                Assert.That(
                    receivedMetadata.Transport,
                    Is.EqualTo("udp"));
                Assert.That(
                    receivedMetadata.RemoteEndpoint,
                    Is.Not.Empty);
                Assert.That(host.IsRunning, Is.True);
            }
        }

        private static int ReserveUdpPort()
        {
            using (var reservation =
                   new UdpClient(
                       new IPEndPoint(
                           IPAddress.Loopback,
                           0)))
            {
                return ((IPEndPoint)
                    reservation.Client.LocalEndPoint).Port;
            }
        }

        private static async Task<T> WithTimeout<T>(
            Task<T> task)
        {
            Task completed =
                await Task.WhenAny(
                    task,
                    Task.Delay(TimeSpan.FromSeconds(3)));
            Assert.That(
                completed,
                Is.SameAs(task),
                "UDP operation timed out.");
            return await task;
        }

        private sealed class PingHandler :
            ICommandHandler<object>
        {
            private static readonly IReadOnlyList<string>
                Names = new[] { "ping" };

            private readonly TaskCompletionSource<
                CommandMetadata> metadata;

            public PingHandler(
                TaskCompletionSource<CommandMetadata>
                    receivedMetadata)
            {
                metadata = receivedMetadata;
            }

            public IReadOnlyList<string> CommandNames =>
                Names;

            public Task<CommandResult> HandleAsync(
                CommandExecutionContext<object> context,
                CancellationToken cancellationToken)
            {
                metadata.TrySetResult(
                    context.Command.Metadata);
                return Task.FromResult(
                    CommandResult.Success(
                        new JObject
                        {
                            ["pong"] = true
                        }));
            }
        }
    }
}
