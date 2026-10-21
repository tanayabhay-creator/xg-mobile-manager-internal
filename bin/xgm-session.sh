# Sourced by egpu-enable and egpu-eject. Defines functions only.
# A login session that started while /etc/environment held the dock variables keeps
# those variables. unset-environment cannot remove them.

XGM_STUCK_VARS=(
  MESA_VK_DEVICE_SELECT
  VK_ICD_FILENAMES
  WLR_DRM_DEVICES
  VK_LOADER_LAYERS_DISABLE
  GAMESCOPE_DISABLE_VK_KHR_PRESENT_WAIT
  DXVK_FILTER_DEVICE_NAME
  VKD3D_FILTER_DEVICE_NAME
  VULKAN_ADAPTER
  OUTPUT_CONNECTOR
  PROTON_ENABLE_NVAPI
  KWIN_DRM_DEVICES
)

xgm_user_systemctl() {
  local user="$1" uid="$2" gid
  shift 2
  if command -v setpriv >/dev/null 2>&1; then
    gid=$(id -g "$user" 2>/dev/null)
    timeout -s KILL 2 setpriv --reuid="$uid" --regid="${gid:-$uid}" --init-groups \
      env XDG_RUNTIME_DIR="/run/user/$uid" systemctl --user "$@"
  else
    timeout -s KILL 2 sudo -u "$user" XDG_RUNTIME_DIR="/run/user/$uid" timeout -s KILL 2 systemctl --user "$@"
  fi
}

xgm_manager_state() {
  systemctl show -p ActiveState -p InvocationID "user@$1.service" 2>/dev/null \
    | awk -F= '$1 == "ActiveState" { s = $2 } $1 == "InvocationID" { i = $2 } END { print s, i }'
}

xgm_stuck_vars() {
  local line name value
  while IFS= read -r line; do
    name="${line%%=*}"
    value="${line#*=}"
    case "$name" in
      GBM_BACKEND)
        [ "$value" = "nvidia-drm" ] && echo "$name"
        ;;
      __GLX_VENDOR_LIBRARY_NAME)
        [ "$value" = "nvidia" ] && echo "$name"
        ;;
      *)
        local var
        for var in "${XGM_STUCK_VARS[@]}"; do
          [ "$name" = "$var" ] && echo "$name"
        done
        ;;
    esac
  done
}

xgm_end_stuck_units() {
  local user="$1" uid="$2" id="$3" doc="/run/user/$uid/doc" unit state cur
  read -r state cur < <(xgm_manager_state "$uid")
  [ "$state" = "deactivating" ] && [ "$cur" = "$id" ] || return 1
  if findmnt -n "$doc" >/dev/null 2>&1; then
    timeout -s KILL 2 fusermount3 -uz "$doc" || true
  fi
  for unit in $(xgm_user_systemctl "$user" "$uid" list-units --state=deactivating --no-legend --plain 2>/dev/null | awk '{ print $1 }'); do
    read -r state cur < <(xgm_manager_state "$uid")
    [ "$state" = "deactivating" ] && [ "$cur" = "$id" ] || return 1
    xgm_user_systemctl "$user" "$uid" kill -s KILL -- "$unit" || true
    echo "Ended a stuck login unit: $unit"
  done
}

xgm_restart_dirty_user_manager() {
  local user="$1" uid state id env_out vars old_id
  local start now state2 id2 deact_since=0 step1=0
  uid=$(id -u "$user" 2>/dev/null) || return 0
  read -r state id < <(xgm_manager_state "$uid")
  [ "$state" = "active" ] || return 0
  if ! env_out=$(xgm_user_systemctl "$user" "$uid" show-environment 2>&1); then
    echo "The login session's environment could not be read. Restarting it so the next login starts clean."
    vars="unknown"
  else
    vars=$(printf '%s\n' "$env_out" | xgm_stuck_vars | xargs)
    [ -z "$vars" ] && return 0
    echo "Dock variables are still in the login session ($vars). Restarting it so the next login starts clean."
  fi
  old_id="$id"
  systemctl stop --no-block "user@$uid.service" || true
  start=$(date +%s)
  while :; do
    read -r state2 id2 < <(xgm_manager_state "$uid")
    if [ "$id2" != "$old_id" ] || [ "$state2" = "inactive" ] || [ "$state2" = "failed" ]; then
      echo "Old login session stopped."
      return 0
    fi
    now=$(date +%s)
    if [ "$state2" = "deactivating" ] && [ "$id2" = "$old_id" ]; then
      if [ "$deact_since" -eq 0 ]; then
        deact_since=$now
      elif [ "$step1" -eq 0 ] && [ $((now - deact_since)) -ge 5 ]; then
        echo "Old login session is still stopping. Ending its stuck units."
        xgm_end_stuck_units "$user" "$uid" "$old_id" || true
        step1=1
        deact_since=$(date +%s)
      elif [ "$step1" -eq 1 ] && [ $((now - deact_since)) -ge 5 ]; then
        read -r state2 id2 < <(xgm_manager_state "$uid")
        if [ "$state2" = "deactivating" ] && [ "$id2" = "$old_id" ]; then
          echo "Old login session is still stopping. Killing it."
          systemctl kill -s KILL "user@$uid.service" || true
        fi
        step1=2
      fi
    fi
    if [ $((now - start)) -ge 15 ]; then
      echo "Login session still running after 15s. Continuing."
      return 0
    fi
    sleep 0.25
  done
}

xgm_new_session() {
  local user="$1" known=" $2 " id props
  for id in $(loginctl list-sessions --no-legend 2>/dev/null | awk -v u="$user" '$3 == u { print $1 }'); do
    [ -n "${known// }" ] && [[ "$known" == *" $id "* ]] && continue
    props=$(loginctl show-session "$id" -p Class -p State 2>/dev/null)
    grep -qx "Class=user" <<< "$props" || continue
    grep -qxE "State=(active|online)" <<< "$props" || continue
    echo "$id"
    return 0
  done
  return 1
}

xgm_wait_for_user_session() {
  local user="$1" known="${2:-}" uid start now session state hung_id="" hung_now
  local deact_since=0 step1=0 jobs
  uid=$(id -u "$user" 2>/dev/null) || return 0
  start=$(date +%s)
  while :; do
    read -r state hung_now < <(xgm_manager_state "$uid")
    now=$(date +%s)
    if [ "$state" = "deactivating" ]; then
      if [ "$hung_id" != "$hung_now" ]; then
        hung_id=$hung_now
        deact_since=$now
        step1=0
      elif [ "$step1" -eq 0 ] && [ $((now - deact_since)) -ge 5 ]; then
        echo "Old login session is still stopping. Ending its stuck units."
        xgm_end_stuck_units "$user" "$uid" "$hung_id" || true
        step1=1
        deact_since=$(date +%s)
      elif [ "$step1" -eq 1 ] && [ $((now - deact_since)) -ge 5 ]; then
        read -r state hung_now < <(xgm_manager_state "$uid")
        if [ "$state" = "deactivating" ] && [ "$hung_now" = "$hung_id" ]; then
          echo "Old login session is still stopping. Killing it."
          systemctl kill -s KILL "user@$uid.service" || true
        fi
        step1=2
      fi
    else
      deact_since=0
      step1=0
      hung_id=""
    fi
    jobs="pending"
    if session=$(xgm_new_session "$user" "$known") \
      && [ "$state" = "active" ] \
      && [ -S "/run/user/$uid/bus" ] \
      && xgm_user_systemctl "$user" "$uid" is-active --quiet graphical-session.target \
      && jobs=$(xgm_user_systemctl "$user" "$uid" list-jobs --no-legend 2>/dev/null) \
      && [ -z "$jobs" ]; then
      echo "Login session ready (session $session)."
      return 0
    fi
    if [ $((now - start)) -ge 60 ]; then
      echo "Login session was not ready after 60s. Continuing."
      return 0
    fi
    sleep 0.25
  done
}

xgm_select_gamescope() {
  local user="$1" uid attempt err code
  uid=$(id -u "$user" 2>/dev/null) || {
    echo "No logged-in user. Game Mode was not selected."
    return 0
  }
  xgm_wait_for_user_session "$user" "${XGM_KNOWN_SESSIONS:-}"
  for attempt in 1 2 3; do
    echo "Selecting Game Mode (attempt $attempt)."
    err=$(sudo -u "$user" XDG_RUNTIME_DIR="/run/user/$uid" DBUS_SESSION_BUS_ADDRESS="unix:path=/run/user/$uid/bus" /usr/bin/steamos-session-select gamescope 2>&1)
    code=$?
    [ -n "$err" ] && echo "$err"
    if [ "$code" -eq 0 ]; then
      return 0
    fi
    if ! printf '%s\n' "$err" | grep -qE 'TransactionIsDestructive|NameHasNoOwner|concurrent deactivation|No such file or directory'; then
      echo "Game Mode selection failed."
      return 0
    fi
    [ "$attempt" -lt 3 ] && sleep 3
  done
  echo "Game Mode selection failed. From Desktop, run: steamos-session-select gamescope"
  return 0
}
