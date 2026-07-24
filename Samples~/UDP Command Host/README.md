# UDP Command Host

1. Create Command Routing and UDP settings from their Deucarian editor
   windows.
2. Add `UdpCommandHostSample` to a GameObject and assign both assets.
3. Enter Play Mode.
4. Use the Python client to send:

```python
client.send("set_label", {"value": "Connected"})
```

The sample creates and disposes one explicit composition root. The handler
knows nothing about UDP.
