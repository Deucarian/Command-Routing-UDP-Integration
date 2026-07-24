using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.CommandRouting;
using Deucarian.Diagnostics;
using Deucarian.Logging;

namespace Deucarian.CommandRouting.UdpIntegration
{
    public sealed class UdpCommandTransport :
        ICommandTransport
    {
        private static readonly DLog Log =
            DLog.For("CommandRouting.UDP");
        private static long nextInstanceId;

        private readonly object lifecycleLock = new object();
        private readonly ConcurrentDictionary<
            string,
            IPEndPoint> knownEndpoints =
                new ConcurrentDictionary<string, IPEndPoint>(
                    StringComparer.Ordinal);
        private readonly UdpCommandTransportOptions options;
        private readonly SynchronizationContext dispatchContext;
        private readonly UdpCommandTransportDiagnostics diagnostics;
        private readonly DiagnosticProviderRegistration
            diagnosticsRegistration;

        private CancellationTokenSource cancellation;
        private UdpClient client;
        private bool running;
        private bool disposed;

        public UdpCommandTransport(
            UdpCommandTransportOptions transportOptions,
            SynchronizationContext messageDispatchContext = null)
        {
            options =
                transportOptions ??
                throw new ArgumentNullException(
                    nameof(transportOptions));
            dispatchContext =
                messageDispatchContext ??
                SynchronizationContext.Current;
            string instanceId =
                Interlocked.Increment(ref nextInstanceId)
                    .ToString();
            diagnostics =
                new UdpCommandTransportDiagnostics(
                    instanceId,
                    options);
            diagnosticsRegistration =
                DiagnosticProviderRegistry.Register(
                    diagnostics);
        }

        public string TransportId => "udp";

        public bool IsRunning
        {
            get
            {
                lock (lifecycleLock)
                {
                    return running;
                }
            }
        }

        public event EventHandler<
            CommandTransportMessageEventArgs> MessageReceived;

        public void Start()
        {
            lock (lifecycleLock)
            {
                ThrowIfDisposed();
                if (running)
                {
                    return;
                }

                var endpoint =
                    new IPEndPoint(
                        options.BindAddress,
                        options.Port);
                client = new UdpClient(endpoint);
                cancellation =
                    new CancellationTokenSource();
                running = true;
                diagnostics.SetRunning(true);
                _ = Task.Run(
                    () => ReceiveLoopAsync(
                        client,
                        cancellation.Token));
            }

            Log.Info(
                "UDP command transport started on " +
                options.BindAddress +
                ":" +
                options.Port +
                ".");
        }

        public void Stop()
        {
            CancellationTokenSource source;
            UdpClient activeClient;
            lock (lifecycleLock)
            {
                if (!running)
                {
                    return;
                }

                running = false;
                source = cancellation;
                activeClient = client;
                cancellation = null;
                client = null;
                diagnostics.SetRunning(false);
            }

            source.Cancel();
            activeClient.Close();
            source.Dispose();
            Log.Info("UDP command transport stopped.");
        }

        public async Task SendAsync(
            string message,
            string remoteEndpoint,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            if (bytes.Length > options.MaximumDatagramBytes)
            {
                diagnostics.RecordDropped();
                throw new InvalidOperationException(
                    "The encoded UDP response exceeds the configured limit.");
            }

            if (!knownEndpoints.TryGetValue(
                    remoteEndpoint ?? string.Empty,
                    out IPEndPoint endpoint))
            {
                diagnostics.RecordDropped();
                throw new InvalidOperationException(
                    "The UDP response endpoint is unavailable.");
            }

            UdpClient activeClient;
            lock (lifecycleLock)
            {
                ThrowIfDisposed();
                if (!running || client == null)
                {
                    throw new InvalidOperationException(
                        "The UDP command transport is not running.");
                }

                activeClient = client;
            }

            await activeClient.SendAsync(
                    bytes,
                    bytes.Length,
                    endpoint)
                .ConfigureAwait(false);
            diagnostics.RecordSent();
        }

        public void Dispose()
        {
            lock (lifecycleLock)
            {
                if (disposed)
                {
                    return;
                }
            }

            Stop();
            lock (lifecycleLock)
            {
                disposed = true;
            }

            diagnosticsRegistration.Dispose();
        }

        private async Task ReceiveLoopAsync(
            UdpClient activeClient,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult packet =
                        await activeClient.ReceiveAsync()
                            .ConfigureAwait(false);
                    if (packet.Buffer.Length >
                        options.MaximumDatagramBytes)
                    {
                        diagnostics.RecordDropped();
                        Log.Warning(
                            "Dropped an oversized UDP command datagram. " +
                            "Payload contents were omitted.");
                        continue;
                    }

                    string endpoint =
                        packet.RemoteEndPoint.ToString();
                    knownEndpoints[endpoint] =
                        packet.RemoteEndPoint;
                    diagnostics.RecordReceived();
                    Publish(
                        Encoding.UTF8.GetString(packet.Buffer),
                        endpoint,
                        cancellationToken);
                }
                catch (ObjectDisposedException)
                    when (cancellationToken
                        .IsCancellationRequested)
                {
                    return;
                }
                catch (SocketException)
                    when (cancellationToken
                        .IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    diagnostics.RecordError(
                        exception.GetType().Name);
                    Log.Error(
                        "UDP receive failed with " +
                        exception.GetType().Name +
                        ". Datagram contents and exception text were omitted.");
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(
                                TimeSpan.FromMilliseconds(100),
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private void Publish(
            string message,
            string endpoint,
            CancellationToken cancellationToken)
        {
            var args =
                new CommandTransportMessageEventArgs(
                    message,
                    endpoint);
            if (dispatchContext == null)
            {
                RaiseMessageReceived(
                    args,
                    cancellationToken);
                return;
            }

            dispatchContext.Post(
                state => RaiseMessageReceived(
                    (CommandTransportMessageEventArgs)state,
                    cancellationToken),
                args);
        }

        private void RaiseMessageReceived(
            CommandTransportMessageEventArgs args,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested ||
                !IsRunning)
            {
                return;
            }

            MessageReceived?.Invoke(this, args);
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
