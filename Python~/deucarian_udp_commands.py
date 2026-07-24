"""Dependency-free UDP client for Deucarian Command Routing."""

from __future__ import annotations

import json
import socket
import uuid
from typing import Any, Dict, Optional


class UdpCommandClient:
    """Sends JSON command envelopes to a Deucarian UDP host."""

    def __init__(
        self,
        host: str = "127.0.0.1",
        port: int = 9050,
        timeout_seconds: float = 2.0,
    ) -> None:
        self._endpoint = (host, port)
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self._socket.settimeout(timeout_seconds)

    def send(
        self,
        command: str,
        payload: Optional[Dict[str, Any]] = None,
        *,
        command_id: Optional[str] = None,
        wait_for_response: bool = True,
    ) -> Optional[Dict[str, Any]]:
        envelope = {
            "protocol_version": 1,
            "command_id": command_id or str(uuid.uuid4()),
            "command": command,
            "payload": payload or {},
            "metadata": {"source": "python", "transport": "udp"},
        }
        message = json.dumps(
            envelope,
            separators=(",", ":"),
        ).encode("utf-8")
        self._socket.sendto(message, self._endpoint)
        if not wait_for_response:
            return None

        response, _ = self._socket.recvfrom(65507)
        return json.loads(response.decode("utf-8"))

    def close(self) -> None:
        self._socket.close()

    def __enter__(self) -> "UdpCommandClient":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


if __name__ == "__main__":
    with UdpCommandClient() as client:
        print(client.send("example_command", {}))
