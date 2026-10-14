"""Sequenced MCP session against a GroupDocs.Search image: initialize -> tools/list ->
add_to_index -> search "lorem". Used by 04_run-server-with-samples.sh.

usage: python smoke_session.py <image> <host-folder-mounted-at-/data>
exit 0 when 8 tools are listed and "lorem" matches at least the three Lorem ipsum samples.
"""
import json
import os
import subprocess
import sys

image, host = sys.argv[1], sys.argv[2]
# The image runs as uid 1654; on Linux a bind mount keeps its host owner, so run as the host user
# or the index cannot be written. Docker Desktop (Windows, macOS) does not enforce ownership.
user = ["--user", f"{os.getuid()}:{os.getgid()}"] if sys.platform.startswith("linux") else []
p = subprocess.Popen(["docker", "run", "--rm", "-i", *user, "-v", f"{host}:/data", image],
                     stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                     text=True, encoding="utf-8")


def send(message):
    p.stdin.write(json.dumps(message) + "\n")
    p.stdin.flush()


def read(request_id):
    while True:
        line = p.stdout.readline()
        if not line:
            sys.exit("server exited early")
        if line.strip().startswith("{"):
            message = json.loads(line)
            if message.get("id") == request_id:
                return message


def call(request_id, name, arguments):
    send({"jsonrpc": "2.0", "id": request_id, "method": "tools/call",
          "params": {"name": name, "arguments": arguments}})
    return read(request_id)["result"]["content"][0]["text"]


send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
    "protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": {"name": "smoke", "version": "1"}}})
print("server:", read(1)["result"]["serverInfo"])
send({"jsonrpc": "2.0", "method": "notifications/initialized"})
send({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
tools = sorted(t["name"] for t in read(2)["result"]["tools"])
print(f"tools ({len(tools)}):", ", ".join(tools))
print("add_to_index:", call(3, "add_to_index", {}).splitlines()[0])
result = json.loads(call(4, "search", {"query": "lorem"}))
print(f"search 'lorem': {result['documentsFound']} document(s):",
      ", ".join(d["filePath"] for d in result["documents"]))
p.stdin.close()
p.wait(timeout=60)
sys.exit(0 if len(tools) == 8 and result["documentsFound"] >= 3 else 1)
