using System;
using System.Collections.Generic;
using System.Threading;
using Deucarian.CommandRouting;

namespace Deucarian.CommandRouting.UdpIntegration
{
    public sealed class UdpCommandRoutingHost<TApplicationContext> :
        IDisposable
    {
        private readonly CommandTransportBridge<TApplicationContext>
            bridge;
        private bool disposed;

        public UdpCommandRoutingHost(
            TApplicationContext applicationContext,
            IEnumerable<ICommandHandler<TApplicationContext>>
                handlers,
            UdpCommandTransportSettings udpSettings = null,
            CommandRoutingSettings routingSettings = null,
            IEnumerable<ICommandMiddleware<TApplicationContext>>
                middleware = null,
            ICommandProtocolCodec protocolCodec = null,
            SynchronizationContext dispatchContext = null)
            : this(
                applicationContext,
                handlers,
                UdpCommandTransportOptions.From(udpSettings),
                CommandRoutingOptions.From(routingSettings),
                middleware,
                protocolCodec,
                dispatchContext)
        {
        }

        public UdpCommandRoutingHost(
            TApplicationContext applicationContext,
            IEnumerable<ICommandHandler<TApplicationContext>>
                handlers,
            UdpCommandTransportOptions udpOptions,
            CommandRoutingOptions routingOptions = null,
            IEnumerable<ICommandMiddleware<TApplicationContext>>
                middleware = null,
            ICommandProtocolCodec protocolCodec = null,
            SynchronizationContext dispatchContext = null)
        {
            UdpOptions =
                udpOptions ??
                throw new ArgumentNullException(
                    nameof(udpOptions));
            ICommandProtocolCodec selectedCodec =
                protocolCodec ??
                CreateCodec(UdpOptions.MessageFormat);
            Runtime =
                new CommandRoutingRuntime<TApplicationContext>(
                    applicationContext,
                    handlers,
                    routingOptions ??
                    new CommandRoutingOptions(),
                    middleware,
                    selectedCodec);
            Transport =
                new UdpCommandTransport(
                    UdpOptions,
                    dispatchContext);
            bridge =
                new CommandTransportBridge<TApplicationContext>(
                    Runtime,
                    Transport,
                    UdpOptions.SendResponses,
                    disposeTransport: true);
        }

        public UdpCommandTransportOptions UdpOptions { get; }

        public CommandRoutingRuntime<TApplicationContext>
            Runtime { get; }

        public UdpCommandTransport Transport { get; }

        public bool IsRunning => bridge.IsRunning;

        public void Start()
        {
            ThrowIfDisposed();
            bridge.Start();
        }

        public void Stop()
        {
            if (disposed)
            {
                return;
            }

            bridge.Stop();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            bridge.Dispose();
            Runtime.Dispose();
        }

        private static ICommandProtocolCodec CreateCodec(
            UdpCommandMessageFormat messageFormat)
        {
            return messageFormat ==
                   UdpCommandMessageFormat.LegacyPlainText
                ? (ICommandProtocolCodec)
                    new LegacyPlainTextCommandProtocolCodec()
                : new JsonCommandProtocolCodec();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    GetType().Name);
            }
        }
    }
}
