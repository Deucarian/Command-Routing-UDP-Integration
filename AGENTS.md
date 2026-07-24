# Deucarian Command Routing UDP Integration Agent Notes

Package ID: `com.deucarian.command-routing.udp-integration`
Repository: `Deucarian/Command-Routing-UDP-Integration`

Follow the canonical
[Deucarian Architecture Rules](https://github.com/Deucarian/Package-Registry/blob/main/ARCHITECTURE.md).

## Ownership

This package owns:

- UDP socket lifecycle and datagram transport.
- Main-thread delivery through a captured synchronization context.
- UDP transport diagnostics and editor configuration.
- The optional legacy plain-text protocol adapter.
- Dependency-free Python interoperability examples.

Registered capability:

- `command-routing-udp-transport`

This package must not own application commands, domain state, authentication
state, generic command dispatch, service location, or other network
transports.

## Dependencies

- `com.deucarian.command-routing`: mandatory command infrastructure.
- `com.deucarian.logging`: mandatory package logging.
- `com.deucarian.diagnostics`: mandatory operational diagnostics.
- `com.deucarian.editor`: mandatory shared editor shell.
- `com.unity.nuget.newtonsoft-json`: legacy protocol payload mapping.

## Policies

- Dependencies are constructed explicitly; do not add a service locator.
- Never log datagram or command payloads.
- Network work stays off the Unity thread; command delivery is marshalled to
  the captured application context.
- Keep editor actions in
  `Tools > Deucarian > Communication > UDP Command Transport`.
- HoloHelmet is a read-only compatibility reference and is never modified by
  work on this package.

## Validation

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run Unity EditMode tests and `git diff --check` before committing.
