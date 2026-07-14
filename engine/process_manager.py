from __future__ import annotations

import os
import shutil
import subprocess
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path

_CANDIDATE_PATHS = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
]


def find_browser_executable() -> str:
    override = os.environ.get("POC_BROWSER_PATH")
    if override:
        return override

    for path in _CANDIDATE_PATHS:
        if Path(path).exists():
            return path

    raise FileNotFoundError(
        "No Chromium-based browser found. Set POC_BROWSER_PATH to the executable path."
    )


@dataclass
class BrowserProcess:
    process: subprocess.Popen #verifies existence of the process or kills it
    user_data_dir: Path #data directory for the browser profile
    devtools_port: int #http port for cdp

    @property
    def http_endpoint(self) -> str:
        return f"http://127.0.0.1:{self.devtools_port}"

    """terminates the browser nicely, if it doesn't terminate in 5 seconds, kills it. 
    Also removes the user data dir."""
    def terminate(self) -> None:
        if self.process.poll() is None:
            self.process.terminate()
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.process.kill()
        shutil.rmtree(self.user_data_dir, ignore_errors=True)


def launch_browser(headless: bool = True) -> BrowserProcess:
    """Spawns an isolated browser process (own OS process, own profile dir)."""
    executable = find_browser_executable()
    user_data_dir = Path(tempfile.mkdtemp(prefix="poc-profile-")) #unique temporary directory for the browser profile

    args = [
        executable,
        f"--user-data-dir={user_data_dir}", #each browser process gets its own profile directory (isolated info)
        "--remote-debugging-port=0", #use a random available port for DevTools, os will assign a free port
        "--no-first-run", #don't show the first run dialog
        "--no-default-browser-check", #don't check if the default browser is set
    ]
    if headless:
        args.append("--headless=new")

    process = subprocess.Popen( #starts the browser without blocking the main thread, allows interaction with it
        args,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )

    port = _wait_for_devtools_port(user_data_dir / "DevToolsActivePort", process)
    return BrowserProcess(process=process, user_data_dir=user_data_dir, devtools_port=port)

"""Chromium doesn't specify the port in advance, so we have to wait for the DevToolsActivePort file to appear. 
This function polls for the file every 50ms and reads the port from it, with a 10s timeout."""
def _wait_for_devtools_port(
    port_file: Path, process: subprocess.Popen, timeout: float = 10.0
) -> int:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError(f"Browser process exited early (code {process.returncode})")
        if port_file.exists():
            try:
                first_line = port_file.read_text().splitlines()[0]
            except (PermissionError, OSError):
                # the file can be momentarily locked right after Chromium
                # creates it (e.g. antivirus scanning it); just retry
                time.sleep(0.05)
                continue
            return int(first_line)
        time.sleep(0.05)
    raise TimeoutError("Timed out waiting for DevToolsActivePort file")
