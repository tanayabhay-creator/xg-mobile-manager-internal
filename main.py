import asyncio
import subprocess
import os
import json
import decky
import decky_plugin
import shutil
import pwd
import time
import glob
import re
from settings import SettingsManager
from typing import TypeVar
from datetime import datetime

Initialized = False
T = TypeVar("T")
PLUGIN_DIR = os.path.dirname(os.path.realpath(__file__))
HOMEBREW_DIR = os.path.abspath(os.path.join(PLUGIN_DIR, "../../"))
LOG_DIR = os.path.join(HOMEBREW_DIR, "logs")
DATA_DIR = os.path.join(HOMEBREW_DIR, "data", "xgmobile-manager")
ENABLE_LOG = os.path.join(LOG_DIR, "xgmobile_manager_enable_latest.log")
ENABLEDESKTOP_LOG = os.path.join(LOG_DIR, "xgmobile_manager_desktop_enable_request.log")
EJECT_LOG = os.path.join(LOG_DIR, "xgmobile_manager_eject_latest.log")
EJECTDESKTOP_LOG = os.path.join(LOG_DIR, "xgmobile_manager_desktop_eject_request.log")
TRANSITION_LOG = os.path.join(LOG_DIR, "xgmobile_manager_transition.log")
HYBRID_LOG = os.path.join(LOG_DIR, "xgmobile_manager_supergfxd_hybrid.log")
INTEGRATED_LOG = os.path.join(LOG_DIR, "xgmobile_manager_supergfxd_integrated.log")
#REPAIR_LOG = os.path.join(LOG_DIR, "xgmobile_manager_repair.log")
DEBUG_LOG = os.path.join(LOG_DIR, "xgmobile_manager_debug.log")
INSTALL_LOG = os.path.join(LOG_DIR, "xgmobile_manager_install.log")
UNINSTALL_LOG = os.path.join(LOG_DIR, "xgmobile_manager_uninstall.log")
SHORTCUTS_LOG = os.path.join(LOG_DIR, "xgmobile_manager_create_shortcuts.log")
SYNC_LOG = os.path.join(LOG_DIR, "xgmobile_manager_boot_sync.log")
PYTHONERROR_LOG = os.path.join(LOG_DIR, "xgmobile_manager_python_crash.log")

os.makedirs(os.path.join(DATA_DIR, "configs"), exist_ok=True)

def log(txt):
  decky.logger.info(txt)

def warn(txt):
  decky.logger.warn(txt)

def error(txt):
  decky.logger.error(txt)

def _clean_env():
  env = os.environ.copy()
  env.pop("LD_LIBRARY_PATH", None)
  return env

def _run_cmd(args):
  return subprocess.run(args, capture_output=True, text=True, env=_clean_env())

def _read_text(path):
  try:
    with open(path, "r") as handle:
      return handle.read().strip()
  except Exception:
    return ""

def _write_text(path, text):
  try:
    with open(path, "w") as handle:
      handle.write(str(text))
    return ""
  except Exception as exc:
    return str(exc)

def _which(name):
  found = shutil.which(name)
  if found:
    return found
  fallback = f"/usr/bin/{name}"
  return fallback if os.path.exists(fallback) else ""

def _as_int(value):
  if isinstance(value, dict):
    value = next(iter(value.values()), 0)
  return int(float(value))

def _parse_labeled_number(text, label):
  match = re.search(rf"{re.escape(label)}\s*:\s*([0-9.]+)", text or "")
  if not match:
    return 0
  return int(float(match.group(1)))

def _nvidia_power_info():
  smi = _which("nvidia-smi")
  if not smi:
    return None
  result = _run_cmd([smi, "-q", "-d", "POWER"])
  if result.returncode != 0:
    return None
  current = _parse_labeled_number(result.stdout, "Current Power Limit")
  minimum = _parse_labeled_number(result.stdout, "Min Power Limit")
  maximum = _parse_labeled_number(result.stdout, "Max Power Limit")
  if maximum <= 0:
    return None
  return {"min": minimum or 5, "max": maximum, "current": current or maximum}

def _nvidia_clock_info():
  smi = _which("nvidia-smi")
  if not smi:
    return None
  result = _run_cmd([smi, "--query-gpu=clocks.max.graphics", "--format=csv,noheader,nounits"])
  if result.returncode != 0 or not result.stdout.strip():
    return None
  try:
    maximum = int(float(result.stdout.strip().splitlines()[0].strip()))
  except Exception:
    return None
  if maximum < 800:
    return None
  return {"min": 500, "max": maximum}

def _apply_nvidia_power(watts):
  smi = _which("nvidia-smi")
  if not smi:
    return "nvidia-smi was not found"
  info = _nvidia_power_info()
  if info:
    watts = max(info["min"], min(info["max"], int(watts)))
  result = _run_cmd([smi, "-pl", str(int(watts))])
  if result.returncode != 0:
    return (result.stderr or result.stdout or "Failed to set the GPU power limit").strip()
  return ""

def _apply_nvidia_clock(mhz):
  smi = _which("nvidia-smi")
  if not smi:
    return "nvidia-smi was not found"
  if int(mhz) <= 0:
    result = _run_cmd([smi, "-rgc"])
  else:
    floor = min(500, int(mhz))
    result = _run_cmd([smi, "-lgc", f"{floor},{int(mhz)}"])
  if result.returncode != 0:
    return (result.stderr or result.stdout or "Failed to set the GPU clock limit").strip()
  return ""

def _drm_cards():
  cards = []
  for path in glob.glob("/sys/class/drm/card[0-9]*"):
    name = os.path.basename(path)
    if not name.startswith("card") or not name[4:].isdigit():
      continue
    internal = False
    for status in glob.glob(path + "-eDP*") + glob.glob(path + "-DSI*") + glob.glob(path + "-LVDS*"):
      if os.path.isdir(status) and _read_text(os.path.join(status, "status")) == "connected":
        internal = True
    cards.append({
      "path": path,
      "vendor": _read_text(os.path.join(path, "device/vendor")),
      "internal": internal,
    })
  return cards

def _amd_power_info():
  nodes = []
  for card in _drm_cards():
    if card["vendor"] != "0x1002" or card["internal"]:
      continue
    nodes.extend(glob.glob(os.path.join(card["path"], "device/hwmon/hwmon*/power1_cap")))
  if not nodes:
    return None
  folder = os.path.dirname(nodes[0])

  def watts(filename, default=0):
    raw = _read_text(os.path.join(folder, filename))
    try:
      return int(int(raw) / 1000000)
    except Exception:
      return default

  maximum = watts("power1_cap_max")
  minimum = watts("power1_cap_min", 5)
  current = watts("power1_cap")
  if maximum <= 0:
    maximum = max(current, 150)
  if current <= 0:
    current = maximum
  return {"min": max(5, minimum), "max": maximum, "current": current, "nodes": nodes}

def _apply_amd_power(watts):
  info = _amd_power_info()
  if not info:
    return "The AMD eGPU power limit was not found"
  watts = max(info["min"], min(info["max"], int(watts)))
  errors = []
  for node in info["nodes"]:
    err = _write_text(node, watts * 1000000)
    if err:
      errors.append(err)
  return "; ".join(errors)

def _gpu_limit_source():
  nvidia = _nvidia_power_info()
  if nvidia:
    return {"kind": "nvidia", "power": nvidia, "clock": _nvidia_clock_info()}
  amd = _amd_power_info()
  if amd:
    return {"kind": "amd", "power": amd, "clock": None}
  return {"kind": "none", "power": None, "clock": None}

ARMOURY_ROOT = "/sys/class/firmware-attributes/asus-armoury/attributes"
PPT_NAMES = ["ppt_pl1_spl", "ppt_pl2_sppt", "ppt_fppt"]

def _armoury_tdp_info():
  for name in PPT_NAMES:
    base = os.path.join(ARMOURY_ROOT, name)
    current_path = os.path.join(base, "current_value")
    if not os.path.exists(current_path):
      continue
    try:
      current = int(float(_read_text(current_path) or 0))
      minimum = int(float(_read_text(os.path.join(base, "min_value")) or 5))
      maximum = int(float(_read_text(os.path.join(base, "max_value")) or 45))
    except Exception:
      continue
    scale = 1000 if maximum > 500 else 1
    return {
      "min": max(5, int(minimum / scale)),
      "max": max(8, int(maximum / scale)),
      "current": max(1, int((current or maximum) / scale)),
      "scale": scale,
    }
  return None

def _apply_armoury_tdp(watts):
  info = _armoury_tdp_info()
  if not info:
    return "ASUS CPU power attributes were not found"
  watts = max(info["min"], min(info["max"], int(watts)))
  raw = watts * info["scale"]
  wrote = False
  errors = []
  for name in PPT_NAMES:
    path = os.path.join(ARMOURY_ROOT, name, "current_value")
    if not os.path.exists(path):
      continue
    wrote = True
    err = _write_text(path, raw)
    if err:
      errors.append(err)
  if not wrote:
    return "ASUS CPU power attributes were not found"
  return "; ".join(errors)

def _apply_ryzenadj_tdp(watts):
  tool = _which("ryzenadj")
  if not tool:
    return "ryzenadj was not found"
  mw = max(5000, int(watts) * 1000)
  result = _run_cmd([
    tool,
    f"--stapm-limit={mw}",
    f"--fast-limit={mw}",
    f"--slow-limit={mw}",
  ])
  if result.returncode != 0:
    return (result.stderr or result.stdout or "ryzenadj failed").strip()
  return ""

def _ryzenadj_current():
  tool = _which("ryzenadj")
  if not tool:
    return 0
  result = _run_cmd([tool, "--info"])
  match = re.search(r"STAPM LIMIT\s*\|\s*([0-9.]+)", result.stdout or "")
  if not match:
    return 0
  return int(float(match.group(1)))

def _cpu_tdp_bounds():
  info = _armoury_tdp_info()
  if info:
    return info["min"], info["max"], info["current"], True
  if _which("ryzenadj"):
    return 8, 45, _ryzenadj_current() or 25, True
  return 8, 45, 15, False

def _apply_cpu_tdp(watts):
  ok = False
  messages = []
  if _armoury_tdp_info():
    err = _apply_armoury_tdp(watts)
    if err:
      messages.append(err)
    else:
      ok = True
  if _which("ryzenadj"):
    err = _apply_ryzenadj_tdp(watts)
    if err:
      messages.append(err)
    else:
      ok = True
  if ok:
    return ""
  if messages:
    return "; ".join(messages)
  return "CPU power control was not found. Install ryzenadj, or use a kernel with ASUS power attributes."

def _cpu_freq_files(name):
  return glob.glob(f"/sys/devices/system/cpu/cpu[0-9]*/cpufreq/{name}")

def _cpu_clock_info():
  max_files = _cpu_freq_files("cpuinfo_max_freq")
  current_files = _cpu_freq_files("scaling_max_freq")
  if not max_files or not current_files:
    return None
  try:
    maximum = int(int(_read_text(max_files[0])) / 1000)
    current = int(int(_read_text(current_files[0])) / 1000)
  except Exception:
    return None
  if maximum < 1000:
    return None
  minimum = min(1400, maximum)
  return {"min": minimum, "max": maximum, "current": min(max(current, minimum), maximum)}

def _apply_cpu_clock(mhz):
  info = _cpu_clock_info()
  if not info:
    return "CPU clock control was not found"
  target = info["max"] if int(mhz) <= 0 else int(mhz)
  target = max(info["min"], min(info["max"], target))
  errors = []
  for path in _cpu_freq_files("scaling_max_freq"):
    err = _write_text(path, target * 1000)
    if err:
      errors.append(err)
      break
  return errors[0] if errors else ""

_last_limit_error = ""

def _note_limit_error(message):
  global _last_limit_error
  if message and message != _last_limit_error:
    _last_limit_error = message
    error(f"Power limit: {message}")
  elif not message:
    _last_limit_error = ""

class Plugin:
  settings: SettingsManager

  # Get the path where the plugin is installed
  def get_plugin_dir(self):
    return PLUGIN_DIR

  async def get_version(self):
    """Reads the version directly from package.json."""
    try:
      json_path = os.path.join(self.get_plugin_dir(), "package.json")
      
      with open(json_path, 'r') as f:
        data = json.load(f)
        return data.get('version', '0.2.0')
    except Exception as e:
      error(f"Error reading version: {e}")
      return "0.2.0"

  async def get_device_type(self):
    try:
      with open('/sys/class/dmi/id/product_name', 'r') as f:
        product_name = f.read().strip()
              
      if "RC71L" in product_name or "Ally" in product_name:
        return "handheld"
      elif "Flow" in product_name or "GV" in product_name or "GZ" in product_name:
        return "laptop"
      else:
        return "unknown"
    except Exception as e:
      return "unknown"

  def get_os_type(self):
    """Detects the host OS and validates the Bazzite NVIDIA image."""
    try:
      with open("/etc/os-release", "r") as f:
        os_data = f.read().lower()
                
        if "bazzite" in os_data:
          # Check if they actually installed the NVIDIA variant
          if "nvidia" not in os_data:
            return "bazzite"
          return "bazzite-nvidia"
        elif "cachyos" in os_data:
          return "cachyos"
        elif "steamos" in os_data:
          return "steamos"
        else:
          return "unsupported"
    except Exception:
      return "unsupported"

  async def get_os_status(self):
    """Helper to pass the OS type to React on load."""
    return self.get_os_type()

  async def has_supergfxctl(self):
    """Returns True if supergfxctl is installed and in the system PATH."""
    return shutil.which("supergfxctl") is not None

  async def create_desktop_shortcuts(self):
    vendor = await self.get_setting("gpu_vendor", "nvidia")
    os_type = self.get_os_type()
    
    return await self._execute_script("create-shortcuts", SHORTCUTS_LOG, vendor, os_type, LOG_DIR, DATA_DIR)

  async def _execute_script(self, script_name, log_path, *args):
    """
    Unified executor that redirects output to a specific log file.
    script_name: name of script in bin/
    log_path: full path to the .log file
    *args: any additional arguments to pass to the script (like vendor)
    """
    script_path = os.path.join(self.get_plugin_dir(), "bin", script_name)
    log(f"Executing: {script_path} with args: {args} Logging to: {log_path}")
        
    if not os.path.exists(script_path):
      error(f"Script not found: {script_path}")
      return f"Error: {script_name} not found"

    clean_env = os.environ.copy()
    clean_env.pop("LD_LIBRARY_PATH", None)
    clean_env.pop('http_proxy', None)
    clean_env.pop('https_proxy', None)
    clean_env.pop('ALL_PROXY', None)

    try:
      with open(log_path, "w") as f:
        f.write(f"--- Script Started: {script_name} at {datetime.now().strftime('%Y-%m-%d %H:%M:%S')} ---\n")
        
        process = await asyncio.create_subprocess_exec(
          'bash', script_path, *args,
          stdout=f, 
          stderr=f, 
          env=clean_env
        )
        
        await process.wait()
            
      return "Success" if process.returncode == 0 else f"Failed (Code {process.returncode})"
    except Exception as e:
      error(f"Execution Error: {str(e)}")
      return f"Error: {str(e)}"

  async def _display_target(self):
    target = await self.get_setting("display_target", "external")
    if target not in ("internal", "external"):
      return "external"
    return target

  async def enable_egpu(self):
    vendor = await self.get_setting("gpu_vendor", "nvidia")
    os_type = self.get_os_type()
    display_target = await self._display_target()

    if vendor == "nvidia" and os_type == "bazzite":
      return "Error: Wrong OS Image. Please use the bazzite-nvidia-deck image."
    return await self._execute_script("egpu-enable", ENABLE_LOG, vendor, os_type, LOG_DIR, DATA_DIR, display_target)

  async def eject_egpu(self):
    vendor = await self.get_setting("gpu_vendor", "nvidia")
    os_type = self.get_os_type()
    
    return await self._execute_script("egpu-eject", EJECT_LOG, vendor, os_type, LOG_DIR, DATA_DIR)

  async def watch_eject_request(self):
    request_file = "/tmp/xgmobile_eject_request"
    fallback_log = PYTHONERROR_LOG #"/tmp/xgmobile_python_crash.log"
    
    while True:
      if os.path.exists(request_file):
        # 1. DELETE IMMEDIATELY to prevent infinite error loops
        os.remove(request_file)
        
        try:
          # 2. Grab the variables directly from the backend, no JSON needed!
          vendor = await self.get_setting("gpu_vendor", "nvidia")
          os_type = self.get_os_type()

          # 3. Safe dynamic user resolution
          decky_user = os.environ.get("DECKY_USER")
          target_user = None
        
          if decky_user:
            try: target_user = pwd.getpwnam(decky_user)
            except KeyError: pass
        
          if not target_user:
            try:
              res = subprocess.run(["pgrep", "-x", "steam"], capture_output=True, text=True)
              if res.returncode == 0 and res.stdout.strip():
                pid = res.stdout.strip().split()[0]
                uid = os.stat(f"/proc/{pid}").st_uid
                target_user = pwd.getpwuid(uid)
            except Exception:
              pass
        
          if not target_user:
            for p in pwd.getpwall():
              if 1000 <= p.pw_uid < 65534 and p.pw_dir.startswith('/home/'):
                target_user = p
                break
        
          if not target_user:
            target_user = pwd.getpwnam("deck")
            
          username = target_user.pw_name
          home_dir = target_user.pw_dir
          desktop_log_path = EJECTDESKTOP_LOG #f"{home_dir}/homebrew/logs/xgmobile_desktop_eject_request.log"
        
          def write_log(message):
            with open(desktop_log_path, "a") as f:
              f.write(f"[{datetime.now().strftime('%H:%M:%S')}] {message}\n")

          write_log("-----------------------------------------")
          write_log(f"Desktop Icon triggered Eject for user: {username} | Vendor: {vendor} | OS: {os_type}")
        
          # 4. Use dynamic plugin paths instead of hardcoded strings
          script_path = os.path.join(self.get_plugin_dir(), "bin", "egpu-eject")
          
          sterile_env = {
            "PATH": "/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin",
            "USER": username,
            "HOME": home_dir
          }

          result = subprocess.run([
            script_path, 
            vendor, os_type, LOG_DIR, DATA_DIR
          ], capture_output=True, text=True, check=False, env=sterile_env)

          desktopdefault = await self.get_setting("desktop_default", "0")
          #if desktopdefault == "1":
          uid = target_user.pw_uid
          write_log(f"Eject Desktop Link detected Desktop Default enabled for : {username}")
          if os_type != "steamos":
            write_log(f"Telling python to wait 5 seconds before sending gamescope command to switch back to GameMode")
            await asyncio.sleep(5)
          write_log(f"Running the command...")
          safe_env = {
            "PATH": "/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin",
            "USER": username,
            "HOME": home_dir,
            "XDG_RUNTIME_DIR": f"/run/user/{uid}",
            "DBUS_SESSION_BUS_ADDRESS": f"unix:path=/run/user/{uid}/bus"
          }
          result = subprocess.run(
            ["sudo", "-E", "-u", username, "/usr/bin/steamos-session-select", "gamescope"],
            capture_output=True,
            text=True,
            check=False,
            env=safe_env
          )

          write_log(f"Command exited with code: {result.returncode}")
          if result.stdout: write_log("STDOUT:\n" + result.stdout.strip())
          if result.stderr: write_log("STDERR:\n" + result.stderr.strip())
        
        except Exception as e:
          # Bleed the error to the fallback log, but DO NOT return. Let the loop survive.
          with open(fallback_log, "a") as f:
            f.write(f"[{datetime.now().strftime('%H:%M:%S')}] CRITICAL WATCHER FAILURE: {str(e)}\n")
            
      # Check every 2 seconds
      await asyncio.sleep(2)

  async def watch_enable_request(self):
    request_file = "/tmp/xgmobile_enable_request"
    fallback_log = PYTHONERROR_LOG #"/tmp/xgmobile_python_crash.log"
    
    while True:
      if os.path.exists(request_file):
        # 1. DELETE IMMEDIATELY to prevent infinite error loops
        os.remove(request_file)
        
        try:
          # 2. Grab the variables directly from the backend, no JSON needed!
          vendor = await self.get_setting("gpu_vendor", "nvidia")
          os_type = self.get_os_type()
          display_target = await self._display_target()

          # 3. Safe dynamic user resolution
          decky_user = os.environ.get("DECKY_USER")
          target_user = None
        
          if decky_user:
            try: target_user = pwd.getpwnam(decky_user)
            except KeyError: pass
        
          if not target_user:
            try:
              res = subprocess.run(["pgrep", "-x", "steam"], capture_output=True, text=True)
              if res.returncode == 0 and res.stdout.strip():
                pid = res.stdout.strip().split()[0]
                uid = os.stat(f"/proc/{pid}").st_uid
                target_user = pwd.getpwuid(uid)
            except Exception:
              pass
        
          if not target_user:
            for p in pwd.getpwall():
              if 1000 <= p.pw_uid < 65534 and p.pw_dir.startswith('/home/'):
                target_user = p
                break
        
          if not target_user:
            target_user = pwd.getpwnam("deck")
            
          username = target_user.pw_name
          home_dir = target_user.pw_dir
          desktop_log_path = ENABLEDESKTOP_LOG #f"{home_dir}/homebrew/logs/xgmobile_desktop_enable_request.log"
        
          def write_log(message):
            with open(desktop_log_path, "a") as f:
              f.write(f"[{datetime.now().strftime('%H:%M:%S')}] {message}\n")

          write_log("-----------------------------------------")
          write_log(f"Desktop Icon triggered Enable for user: {username} | Vendor: {vendor} | OS: {os_type}")
        
          # 4. Use dynamic plugin paths instead of hardcoded strings
          script_path = os.path.join(self.get_plugin_dir(), "bin", "egpu-enable")
          
          sterile_env = {
            "PATH": "/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin",
            "USER": username,
            "HOME": home_dir
          }

          result = subprocess.run([
            script_path, 
            vendor, os_type, LOG_DIR, DATA_DIR, display_target
          ], capture_output=True, text=True, check=False, env=sterile_env)
        
          write_log(f"Command exited with code: {result.returncode}")
          if result.returncode == 0: 
            desktopmode = "1" #await self.get_setting("desktop_mode", "0")
            if desktopmode == "1":
              desktopdefault = "0" #await self.get_setting("desktop_default", "0")
              uid = target_user.pw_uid
              
              if os_type != "steamos":
                write_log(f"Desktop Link Enable detected Boot-To-Desktop. Waiting 5 seconds for Steam to stabilize...")
                await asyncio.sleep(5)

              write_log(f"Executing OS Desktop Transition...")
              
              safe_env = {
                "PATH": "/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin",
                "USER": username,
                "HOME": home_dir,
                "XDG_RUNTIME_DIR": f"/run/user/{uid}",
                "DBUS_SESSION_BUS_ADDRESS": f"unix:path=/run/user/{uid}/bus"
              }
              
              if os_type == "steamos" or os_type == "cachyos":
                target_session = "plasma-wayland-persistent" if desktopdefault == "1" else "plasma-wayland"
              else:
                target_session = "plasma-persistent" if desktopdefault == "1" else "plasma"

              write_log(f"Desktop {os_type} detected, running following session select: {target_session}")

              transition_result = subprocess.run(
                ["sudo", "-E", "-u", username, "/usr/bin/steamos-session-select", target_session],
                capture_output=True, text=True, check=False, env=safe_env
              )
              
              write_log(f"Transition Code: {transition_result.returncode}")
              if transition_result.stdout: write_log("STDOUT:\n" + transition_result.stdout.strip())
              if transition_result.stderr: write_log("STDERR:\n" + transition_result.stderr.strip())
          if result.stdout: write_log("STDOUT:\n" + result.stdout.strip())
          if result.stderr: write_log("STDERR:\n" + result.stderr.strip())
        
        except Exception as e:
          # Bleed the error to the fallback log, but DO NOT return. Let the loop survive.
          with open(fallback_log, "a") as f:
            f.write(f"[{datetime.now().strftime('%H:%M:%S')}] CRITICAL WATCHER FAILURE: {str(e)}\n")
            
      # Check every 2 seconds
      await asyncio.sleep(2)

  async def hybrid_supergfxctl(self):
    vendor = await self.get_setting("gpu_vendor", "nvidia")
    os_type = self.get_os_type()

    if vendor == "nvidia" and os_type == "bazzite":
      return "Error: Wrong OS Image. Please use the bazzite-nvidia-deck image."
    return await self._execute_script("supergfx-hybrid", HYBRID_LOG, vendor, os_type, LOG_DIR, DATA_DIR)

  async def integrated_supergfxctl(self):
    vendor = await self.get_setting("gpu_vendor", "nvidia")
    os_type = self.get_os_type()
    
    return await self._execute_script("supergfx-integrated", INTEGRATED_LOG, vendor, os_type, LOG_DIR, DATA_DIR)

  async def repair_services(self):
    vendor = await self.get_setting("gpu_vendor", "nvidia")
    return await self._execute_script("repair-services", REPAIR_LOG, vendor)

  async def install_nvidia(self):
    if not self.get_os_type() == "steamos":
      log("SteamOS not detected: Blocking DKMS driver installation.")
      return "Error: SteamOS not detected. NVIDIA Driver install is currently only for SteamOS."
        
    log("SteamOS detected: Starting DKMS driver compilation.")
    #return await self._execute_script("install-nvidia.sh", INSTALL_LOG, LOG_DIR, DATA_DIR)
    res = await self._execute_script("install-nvidia.sh", INSTALL_LOG, LOG_DIR, DATA_DIR)
    if "Code 1" in res:
        return "ALREADY_INSTALLED"
    elif res == "Success":
        return "SUCCESS"
    else:
        return "ERROR"

  async def uninstall_nvidia(self):
    return await self._execute_script("uninstall.sh", UNINSTALL_LOG, LOG_DIR, DATA_DIR)
  
  async def set_desktop_transition_flag(self):
    try:
      with open("/var/tmp/xgmobile_desktop_trigger", "w") as f:
        f.write("transition_requested")
      return "SUCCESS: File Written"
    except Exception as e:
      return f"ERROR: {str(e)}"

  async def check_and_clear_desktop_flag(self):
    flag_path = "/var/tmp/xgmobile_desktop_trigger"
    if os.path.exists(flag_path):
      os.remove(flag_path)
      return True
    return False

  async def trigger_desktop_mode(self):
    fallback_log = PYTHONERROR_LOG #"/tmp/xgmobile_python_crash.log"
    
    try:
      # 1. Safe dynamic user resolution
      decky_user = os.environ.get("DECKY_USER")
      target_user = None
        
      if decky_user:
        try: target_user = pwd.getpwnam(decky_user)
        except KeyError: pass
        
      if not target_user:
        try:
          # Use .run() instead of check_output so it doesn't crash if steam is missing
          res = subprocess.run(["pgrep", "-x", "steam"], capture_output=True, text=True)
          if res.returncode == 0 and res.stdout.strip():
            pid = res.stdout.strip().split()[0]
            uid = os.stat(f"/proc/{pid}").st_uid
            target_user = pwd.getpwuid(uid)
        except Exception:
          pass
        
      if not target_user:
        for p in pwd.getpwall():
          if 1000 <= p.pw_uid < 65534 and p.pw_dir.startswith('/home/'):
            target_user = p
            break
        
      if not target_user:
        target_user = pwd.getpwnam("deck") # Absolute last resort
            
      username = target_user.pw_name
      home_dir = target_user.pw_dir
      uid = target_user.pw_uid
      os_type = self.get_os_type()
        
      # Now we have a guaranteed safe home directory to write to
      log_path = TRANSITION_LOG #f"{home_dir}/homebrew/logs/xgmobile_transition.log"
        
      def write_log(message):
        with open(log_path, "a") as f:
          f.write(f"[{datetime.now().strftime('%H:%M:%S')}] {message}\n")

      write_log("-----------------------------------------")
      write_log(f"React triggered OS Desktop Handoff for user: {username}")
      if os_type != "steamos":
        write_log(f"Telling python to wait 5 seconds")
        await asyncio.sleep(5)
      write_log(f"Running the command...")
      #clean_env = os.environ.copy()
      #clean_env.pop("LD_LIBRARY_PATH", None)
        
      #clean_env["USER"] = username
      #clean_env["HOME"] = home_dir
      #clean_env["XDG_RUNTIME_DIR"] = f"/run/user/{uid}"
      #clean_env["DBUS_SESSION_BUS_ADDRESS"] = f"unix:path=/run/user/{uid}/bus"
        
      safe_env = {
        "PATH": "/usr/local/bin:/usr/bin:/bin:/usr/local/sbin:/usr/sbin:/sbin",
        "USER": username,
        "HOME": home_dir,
        "XDG_RUNTIME_DIR": f"/run/user/{uid}",
        "DBUS_SESSION_BUS_ADDRESS": f"unix:path=/run/user/{uid}/bus"
      }
      desktopdefault = await self.get_setting("desktop_default", "0")

      if os_type == "steamos" or os_type == "cachyos":
        target_session = "plasma-wayland-persistent" if desktopdefault == "1" else "plasma-wayland"
      else:
        target_session = "plasma-wayland-persistent" if desktopdefault == "1" else "plasma"

      write_log(f"Desktop {os_type} detected, running following session select: {target_session}")

      result = subprocess.run(
        ["sudo", "-E", "-u", username, "/usr/bin/steamos-session-select", target_session],
        capture_output=True, text=True, check=False, env=safe_env
      )
      
      write_log(f"Command exited with code: {result.returncode}")
        
      if result.stdout:
        write_log("STDOUT:")
        write_log(result.stdout.strip())
            
      if result.stderr:
        write_log("STDERR (ERRORS):")
        write_log(result.stderr.strip())
            
      return True
        
    except Exception as e:
      # If absolutely everything breaks, bleed the error into /tmp/
      with open(fallback_log, "a") as f:
        f.write(f"[{datetime.now().strftime('%H:%M:%S')}] CRITICAL PYTHON FAILURE: {str(e)}\n")
      return False

  async def reboot_system(self):
    clean_env = os.environ.copy()
    clean_env.pop("LD_LIBRARY_PATH", None)
    subprocess.run(["systemctl", "reboot"], env=clean_env)
    return True

  async def restart_supergfxd(self):
    """Safely enables and restarts the Asus daemon for Flow Laptop users."""
    log("Attempting to enable and restart supergfxd.service...")
    clean_env = os.environ.copy()
    clean_env.pop("LD_LIBRARY_PATH", None)
    
    try:
      # Chain enable and restart to guarantee boot persistence and immediate application
      process = await asyncio.create_subprocess_exec(
        'sudo', 'bash', '-c', 'systemctl enable supergfxd.service && systemctl restart supergfxd.service',
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE,
        env=clean_env
      )
      await process.wait()
      
      return "Success" if process.returncode == 0 else f"Failed (Code {process.returncode})"
    except Exception as e:
      error(f"Error enabling supergfxd: {e}")
      return f"Error: {str(e)}"

  async def get_power_profile(self):
    try:
      policy_path = "/sys/devices/platform/asus-nb-wmi/throttle_thermal_policy"
      if not os.path.exists(policy_path):
        return "Unknown"
        
      with open(policy_path, "r") as f:
        val = f.read().strip()
        
      # WMI Mapping: 0 = Balanced, 1 = Performance/Turbo, 2 = Quiet
      if val == "0":
        return "Balanced"
      elif val == "1":
        return "Performance"
      elif val == "2":
        return "Quiet"
      else:
        return "Unknown"
    except Exception as e:
      error(f"Error reading power profile: {e}")
      return "Error"

  async def set_power_profile(self, profile: str):
    if isinstance(profile, dict):
      profile = profile.get("profile", "")
    clean_profile = str(profile).strip().lower()
    log(f"WMI PROFILE RECEIVED FROM UI: '{clean_profile}'")
    
    val_to_write = "0"
    platform_name = "balanced"
    if "balanced" in clean_profile:
      val_to_write = "0"
      platform_name = "balanced"
    elif "performance" in clean_profile:
      val_to_write = "1"
      platform_name = "performance"
    elif "quiet" in clean_profile:
      val_to_write = "2"
      platform_name = "quiet"

    wrote = False
    errors = []
    policy_path = "/sys/devices/platform/asus-nb-wmi/throttle_thermal_policy"
    if os.path.exists(policy_path):
      err = _write_text(policy_path, val_to_write)
      if err:
        errors.append(err)
      else:
        wrote = True

    profile_path = "/sys/firmware/acpi/platform_profile"
    choices = _read_text(profile_path + "_choices").split()
    desired = platform_name
    if desired not in choices and desired == "quiet" and "low-power" in choices:
      desired = "low-power"
    if os.path.exists(profile_path) and desired in choices:
      err = _write_text(profile_path, desired)
      if err:
        errors.append(err)
      else:
        wrote = True

    if wrote:
      return "Success"
    if errors:
      return "The power profile did not change. Use the watt and clock sliders. " + errors[0]
    return "The power profile did not change. Use the watt and clock sliders."

  def _saved_limit(self, key):
    try:
      if not getattr(Plugin, "settings", None):
        return 0
      return int(float(Plugin.settings.getSetting(key, 0) or 0))
    except Exception:
      return 0

  async def get_limit_status(self):
    source = _gpu_limit_source()
    power = source["power"] or {"min": 10, "max": 150, "current": 100}
    clock = source["clock"]
    cpu_min, cpu_max, cpu_current, cpu_ok = _cpu_tdp_bounds()
    clocks = _cpu_clock_info() or {"min": 1400, "max": 5000, "current": 5000}
    saved_power = self._saved_limit("gpu_power_watts")
    saved_clock = self._saved_limit("gpu_clock_mhz")
    saved_tdp = self._saved_limit("cpu_tdp_watts")
    saved_cpu_clock = self._saved_limit("cpu_clock_mhz")

    def clamp(value, low, high):
      return min(high, max(low, value))

    clock_min = clock["min"] if clock else 500
    clock_max = clock["max"] if clock else 2500
    return {
      "gpu_ready": source["kind"] != "none",
      "gpu_kind": source["kind"],
      "gpu_power_min": power["min"],
      "gpu_power_max": power["max"],
      "gpu_power": clamp(saved_power or power["current"], power["min"], power["max"]),
      "gpu_clock_supported": clock is not None,
      "gpu_clock_min": clock_min,
      "gpu_clock_max": clock_max,
      "gpu_clock": clamp(saved_clock or clock_max, clock_min, clock_max),
      "cpu_tdp_supported": cpu_ok,
      "cpu_tdp_min": cpu_min,
      "cpu_tdp_max": cpu_max,
      "cpu_tdp": clamp(saved_tdp or cpu_current, cpu_min, cpu_max),
      "cpu_clock_supported": _cpu_clock_info() is not None,
      "cpu_clock_min": clocks["min"],
      "cpu_clock_max": clocks["max"],
      "cpu_clock": clamp(saved_cpu_clock or clocks["current"], clocks["min"], clocks["max"]),
    }

  async def apply_saved_limits(self):
    messages = []
    gpu_power = self._saved_limit("gpu_power_watts")
    gpu_clock = self._saved_limit("gpu_clock_mhz")
    cpu_tdp = self._saved_limit("cpu_tdp_watts")
    cpu_clock = self._saved_limit("cpu_clock_mhz")
    source = _gpu_limit_source() if gpu_power > 0 or gpu_clock > 0 else {"kind": "none", "power": None, "clock": None}
    if gpu_power > 0:
      if source["kind"] == "nvidia":
        err = _apply_nvidia_power(gpu_power)
      elif source["kind"] == "amd":
        err = _apply_amd_power(gpu_power)
      else:
        err = ""
      if err:
        messages.append(err)
    if gpu_clock > 0 and source["kind"] == "nvidia":
      err = _apply_nvidia_clock(gpu_clock)
      if err:
        messages.append(err)
    if cpu_tdp > 0:
      err = _apply_cpu_tdp(cpu_tdp)
      if err:
        messages.append(err)
    if cpu_clock > 0:
      err = _apply_cpu_clock(cpu_clock)
      if err:
        messages.append(err)
    _note_limit_error("; ".join(messages))
    return "Success" if not messages else "; ".join(messages)

  async def set_gpu_power_limit(self, watts):
    watts = _as_int(watts)
    source = _gpu_limit_source()
    if source["kind"] == "nvidia":
      err = _apply_nvidia_power(watts)
    elif source["kind"] == "amd":
      err = _apply_amd_power(watts)
    else:
      err = "The eGPU is not available yet. Enable the XG Mobile first."
    if err:
      return err
    Plugin.settings.setSetting("gpu_power_watts", watts)
    return "Success"

  async def set_gpu_clock_limit(self, mhz):
    mhz = _as_int(mhz)
    if _gpu_limit_source()["kind"] != "nvidia":
      return "Clock limiting is available for the NVIDIA XG Mobile."
    err = _apply_nvidia_clock(mhz)
    if err:
      return err
    Plugin.settings.setSetting("gpu_clock_mhz", mhz)
    return "Success"

  async def set_cpu_tdp(self, watts):
    watts = _as_int(watts)
    err = _apply_cpu_tdp(watts)
    if err:
      return err
    Plugin.settings.setSetting("cpu_tdp_watts", watts)
    return "Success"

  async def set_cpu_clock_limit(self, mhz):
    mhz = _as_int(mhz)
    err = _apply_cpu_clock(mhz)
    if err:
      return err
    Plugin.settings.setSetting("cpu_clock_mhz", mhz)
    return "Success"

  async def reset_power_limits(self):
    for key in ("gpu_power_watts", "gpu_clock_mhz", "cpu_tdp_watts", "cpu_clock_mhz"):
      Plugin.settings.setSetting(key, 0)
    messages = []
    source = _gpu_limit_source()
    if source["kind"] == "nvidia" and source["power"]:
      err = _apply_nvidia_power(source["power"]["max"])
      if err:
        messages.append(err)
      err = _apply_nvidia_clock(0)
      if err:
        messages.append(err)
    elif source["kind"] == "amd" and source["power"]:
      err = _apply_amd_power(source["power"]["max"])
      if err:
        messages.append(err)
    _, cpu_max, _, cpu_ok = _cpu_tdp_bounds()
    if cpu_ok:
      err = _apply_cpu_tdp(cpu_max)
      if err:
        messages.append(err)
    if _cpu_clock_info():
      err = _apply_cpu_clock(0)
      if err:
        messages.append(err)
    return "Success" if not messages else "; ".join(messages)

  async def _keep_limits(self):
    while True:
      await asyncio.sleep(5)
      try:
        await self.apply_saved_limits()
      except Exception as exc:
        _note_limit_error(str(exc))

  async def get_live_logs(self, log_type="install"):
    """Called by the frontend every 500ms. log_type can be 'repair' or 'install' or 'uninstall'."""
    log_map = {
      "enable": ENABLE_LOG,
      "enabledesktop": ENABLEDESKTOP_LOG,
      "eject": EJECT_LOG,
      "ejectdesktop": EJECTDESKTOP_LOG,
      "transition": TRANSITION_LOG,
      "hybrid": HYBRID_LOG,
      "integrated": INTEGRATED_LOG,
      "install": INSTALL_LOG,
      #"repair": REPAIR_LOG,
      "uninstall": UNINSTALL_LOG,
      "shortcuts": SHORTCUTS_LOG,
      "sync": SYNC_LOG,
      "python": PYTHONERROR_LOG,
      "debug": DEBUG_LOG
    }
    path = log_map.get(log_type, ENABLE_LOG)
    
    if not os.path.exists(path):
      return ""
    try:
      with open(path, "r") as f:
        return f.read()
    except:
      return "Error reading log file."

  async def get_gpu_status(self):
    status = {"connected": False, "active": False, "vendor": "none"}
    
    try:
      with open(DEBUG_LOG, "a") as dbg:

        conn_path = "/sys/devices/platform/asus-nb-wmi/egpu_connected"
        if os.path.exists(conn_path):
          with open(conn_path, "r") as f:
            val = f.read().strip()
            status["connected"] = (val == "1")
        else:
          return "Error: egpu_connected path missing"

        # Check Active Flag
        enable_path = "/sys/devices/platform/asus-nb-wmi/egpu_enable"
        if os.path.exists(enable_path):
          with open(enable_path, "r") as f:
            val = f.read().strip()
            status["active"] = (val == "1")
        else:
          return "Error: egpu_enable path missing"

        # Vendor check (PCI Bus)
        try:
          # Ask for ANY NVIDIA device on the PCI bus (Vendor ID 10de)
          res = subprocess.check_output(["lspci", "-n", "-d", "10de:"]).decode()
          if "10de:" in res:
            status["vendor"] = "nvidia"
        except:
          if os.path.exists("/dev/dri/card1"):
            status["vendor"] = "amd"

    except Exception as e:
      error(f"CRITICAL PYTHON ERROR in get_gpu_status: {e}")
      with open(DEBUG_LOG, "a") as dbg:
        dbg.write(f"CRITICAL ERROR: {e}\n")
      return status

    return status

  async def get_telemetry(self):
    script_path = os.path.join(self.get_plugin_dir(), "bin", "get-gpu-stats")
    
    # 1. Clean environment to ensure nvidia-smi can find its libraries
    clean_env = os.environ.copy()
    clean_env.pop("LD_LIBRARY_PATH", None)

    try:
      result = subprocess.run(
          [script_path],
          capture_output=True,
          text=True,
          env=clean_env
      )
      
      if result.returncode == 0:
          return json.loads(result.stdout)
      else:
          return {"vendor": "none", "temp": "Err", "util": "Err", "vram": "Err", "power": "Err"}
          
    except Exception as e:
      log(f"Telemetry Fetch Error: {e}")
      return {"vendor": "none", "temp": "--", "util": "--", "vram": "--", "power": "--"}

  # --- LOGGING & REPAIR ---

  async def get_latest_logs(self, log_type="enable"):
    """Reads logs for display in a modal."""
    log_map = {
      "enable": ENABLE_LOG,
      "enabledesktop": ENABLEDESKTOP_LOG,
      "eject": EJECT_LOG,
      "ejectdesktop": EJECTDESKTOP_LOG,
      "transition": TRANSITION_LOG,
      "hybrid": HYBRID_LOG,
      "integrated": INTEGRATED_LOG,
      "install": INSTALL_LOG,
      #"repair": REPAIR_LOG,
      "uninstall": UNINSTALL_LOG,
      "shortcuts": SHORTCUTS_LOG,
      "sync": SYNC_LOG,
      "python": PYTHONERROR_LOG,
      "debug": DEBUG_LOG
    }
    
    path = log_map.get(log_type, ENABLE_LOG)

    if not os.path.exists(path):
      return f"No log file found at {path}"

    try:
      with open(path, "r") as f:
        return f.read()
    except Exception as e:
      error(f"Error reading {path}: {e}")
      return f"Error reading log file: {str(e)}"

  async def logMessage(self, message, level):
    if level == 0:
      log(message)
    elif level == 1:
      warn(message)
    elif level == 2:
      error(message)

  # Core Plugin methods
  async def read(self) -> None:
    """
    Reads the json from disk
    """
    Plugin.settings.read()

    # TODO: assign your settings to plugin properties here
  
  # TODO: define additional settings setters here

  # Plugin settingsManager wrappers
  async def get_setting(self, key, default: T) -> T:
    """
    Gets the specified setting from the json

    :param key: The key to get
    :param default: The default value
    :return: The value, or default if not found
    """
    return Plugin.settings.getSetting(key, default)

  async def set_setting(self, key, value: T) -> T:
    """
    Sets the specified setting in the json

    :param key: The key to set
    :param value: The value to set it to
    :return: The new value
    """
    Plugin.settings.setSetting(key, value)
    return value
  
  def del_setting(self, key) -> None:
    """
    Deletes the specified setting in the json
    """
    del Plugin.settings.settings[key]
    Plugin.settings.commit()
    pass

  # Asyncio-compatible long-running code, executed in a task when the plugin is loaded
  async def _main(self):
    global Initialized

    if Initialized:
      return

    Initialized = True

    Plugin.settings = SettingsManager(name="settings", settings_directory=os.environ["DECKY_PLUGIN_SETTINGS_DIR"])
    await Plugin.read(self)
    
    asyncio.create_task(self.watch_eject_request())
    asyncio.create_task(self.watch_enable_request())
    asyncio.create_task(self._keep_limits())

#    try:
#      # Check if the eGPU survived the reboot and is still active
#      gpu_state = await self.get_gpu_status()
#      vendor = await self.get_setting("gpu_vendor", "nvidia")
#      os_type = self.get_os_type()
#      if gpu_state.get("active") and vendor == "nvidia":
#        log("eGPU detected as active on boot. Running DRM node sync...")
#        await self._execute_script("egpu-sync", SYNC_LOG, vendor, os_type)
#    except Exception as e:
#      error(f"Boot DRM sync failed: {e}")

    log("XGMobile-Manager Backend Initialized.")

  # Function called first during the unload process, utilize this to handle your plugin being removed
  async def _unload(self):
    decky.logger.info("Unloading Plugin.")

  # Function called when the plugin is uninstalled
  async def _uninstall(self):
    decky.logger.info("Uninstalling Plugin.")

  # Migrations that should be performed before entering `_main()`.
  async def _migration(self):
    pass
