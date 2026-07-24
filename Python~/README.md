# Python client

`deucarian_udp_commands.py` uses only the Python standard library.

```python
from deucarian_udp_commands import UdpCommandClient

with UdpCommandClient("127.0.0.1", 9050) as client:
    response = client.send("example_command", {})
    print(response)
```

Set `wait_for_response=False` when the Unity host has responses disabled.
