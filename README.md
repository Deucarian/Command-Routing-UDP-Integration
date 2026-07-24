# Deucarian Command Routing UDP Integration

UDP transport and Python interoperability for
`com.deucarian.command-routing`.

The package keeps network I/O outside the command-routing core while reusing
its handler strategies, middleware, redaction, bounded history, logging, and
diagnostics. Incoming datagrams are delivered through a captured
`SynchronizationContext`, allowing Unity applications to execute handlers on
their main thread.

## Install

```json
{
  "dependencies": {
    "com.deucarian.command-routing.udp-integration":
      "https://github.com/Deucarian/Command-Routing-UDP-Integration.git#main"
  }
}
```

## Compose a host

```csharp
host = new UdpCommandRoutingHost<MyApplicationContext>(
    context,
    handlers,
    udpSettings,
    commandRoutingSettings);
host.Start();
```

Dispose the host when its owning application context is disabled or
destroyed. The host composes concrete UDP infrastructure at the application
boundary; handlers continue to depend only on Command Routing abstractions.

## Protocols

- JSON is the default and recommended protocol.
- `LegacyPlainTextCommandProtocolCodec` supports existing tools that send a
  command name followed by an optional text value.
- `Python~/deucarian_udp_commands.py` is a dependency-free Python client for
  JSON commands.

## Editor

Open `Tools > Deucarian > Communication > UDP Command Transport` to create or
inspect settings, copy a Python example, and review package diagnostics.

The package never logs or stores command payloads. Any application result
sent over UDP is encoded and redacted by Command Routing.
