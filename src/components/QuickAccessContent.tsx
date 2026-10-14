import React, { useState, useEffect, useRef } from "react";
import { 
  ButtonItem, 
  Spinner, 
  showModal, 
  ConfirmModal,
  ToggleField, 
  PanelSection, 
  PanelSectionRow,
  Focusable,
  Dropdown,
  SliderField
} from "@decky/ui";
import { call, toaster } from "@decky/api";
import { LiveLogViewerModal } from "./LiveLogViewerModal";
import { LogViewerModal } from "./LogViewerModal";

// Helper for UI styling
const statsStyle: React.CSSProperties = {
  display: "grid",
  gridTemplateColumns: "1fr 1fr",
  gap: "4px",
  padding: "6px",
  backgroundColor: "rgba(0,0,0,0.2)",
  borderRadius: "4px",
  marginBottom: "10px"
};

export const QuickAccessContent = () => {
  const [needsReboot, setNeedsReboot] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [statusText, setStatusText] = useState("");
  const [pluginVersion, setPluginVersion] = useState("Loading...");
  //const [hasSupergfxctl, setHasSupergfxctl] = useState<boolean>(false);
  const [deviceType, setDeviceType] = useState<string>("unknown");
  //const [daemonActive, setDaemonActive] = useState<boolean>(true);
  
  // Dynamic State from Backend
  const [gpuStatus, setGpuStatus] = useState({ connected: false, active: false, vendor: "none" });
  const [telemetry, setTelemetry] = useState({ temp: "--", power: "--", vram: "--", util: "--" });
  const [selectedVendor, setSelectedVendor] = useState("nvidia");
  const [selectedDesktopMode, setSelectedDesktopMode] = useState("0");
  const [selectedDesktopDefault, setSelectedDesktopDefault] = useState("0");
  const [displayTarget, setDisplayTarget] = useState("external");
  const [osType, setOsType] = useState("steamos");
  const [powerProfile, setPowerProfile] = useState("Unknown");
  const [limits, setLimits] = useState<any>(null);
  const [gpuPower, setGpuPower] = useState(100);
  const [gpuClock, setGpuClock] = useState(2000);
  const [cpuTdp, setCpuTdp] = useState(20);
  const [cpuClock, setCpuClock] = useState(4000);
  const limitTimers = useRef<Record<string, number>>({});
  const showSleepWarning = gpuStatus.active && selectedVendor === 'nvidia' && (osType.includes('bazzite') || osType === 'cachyos');

  // 1. Initial Load: Fetch Vendor Setting once
  useEffect(() => {
    const init = async () => {
      try {
        const val = await call("get_setting", "gpu_vendor", "nvidia") as string;
        setSelectedVendor(val);
        const desktopmode = await call("get_setting", "desktop_mode", "0") as string;
        setSelectedDesktopMode(desktopmode);
        const desktopdefault = await call("get_setting", "desktop_default", "0") as string;
        setSelectedDesktopDefault(desktopdefault);
        const display = await call("get_setting", "display_target", "external") as string;
        setDisplayTarget(display === "internal" ? "internal" : "external");
        const ver = await call("get_version") as string;
        if (ver) setPluginVersion(ver);
        const os = await call("get_os_status") as string;
        setOsType(os);
        // call() returns the boolean directly from Python
        //const hasSgfx = await call("has_supergfxctl") as boolean;
        //setHasSupergfxctl(hasSgfx);
        const device = await call("get_device_type") as string;
        setDeviceType(device);
      } catch (e) { 
        console.error("Init Error:", e); 
      }
    };
    init();
  }, []);

  // 2. The Polling Loop: Only fetches hardware/telemetry
  useEffect(() => {
    const poll = async () => {
      try {
        const currentStatus = await call("get_gpu_status") as any;
        if (currentStatus) setGpuStatus(currentStatus);
        if (currentStatus?.active) {
          const stats = await call("get_telemetry") as any;
          if (stats) setTelemetry(stats);
        }
        const prof = await call("get_power_profile") as string;
        setPowerProfile(prof);
      } catch (e) {
        console.error("Poll Error:", e);
      }
    };

    poll(); 
    const interval = setInterval(poll, 3000);
    return () => clearInterval(interval);
  }, []);

  const loadLimits = async () => {
    try {
      const status = await call("get_limit_status") as any;
      if (!status) return;
      setLimits(status);
      setGpuPower(Number(status.gpu_power) || 0);
      setGpuClock(Number(status.gpu_clock) || 0);
      setCpuTdp(Number(status.cpu_tdp) || 0);
      setCpuClock(Number(status.cpu_clock) || 0);
    } catch (e) {
      console.error("Limit status error:", e);
    }
  };

  useEffect(() => {
    loadLimits();
  }, [gpuStatus.active]);

  const queueLimit = (method: string, value: number) => {
    const existing = limitTimers.current[method];
    if (existing) window.clearTimeout(existing);
    limitTimers.current[method] = window.setTimeout(async () => {
      try {
        const result = await call(method, Math.round(value)) as string;
        if (result && result !== "Success") {
          toaster.toast({ title: "Power limit", body: String(result), duration: 5000 });
        }
      } catch (e) {
        toaster.toast({ title: "Power limit", body: "Could not apply the limit." });
      }
    }, 400);
  };

  const handleAction = async (method: string, label: string) => {
    if (isLoading) return;
    setIsLoading(true);
    setStatusText(label);
    toaster.toast({ title: "XG Mobile", body: `Starting: ${label}...` });

    try {
      if (selectedDesktopMode === "1" && method === "enable_egpu") {
        //toaster.toast({ title: "Debug", body: "1. React is calling Python..." });
        
        const flagResult = await call("set_desktop_transition_flag") as string;
        
        toaster.toast({ title: "DesktopMode", body: `Setting Desktop Flag: ${flagResult}` });
        
        //await new Promise(resolve => setTimeout(resolve, 3000));
      }
      const result = await call(method);
      toaster.toast({ title: "XG Mobile", body: `${label} Result: ${result}` });
      const newStatus = await call("get_gpu_status") as any;
      setGpuStatus(newStatus);
    } catch (e) {
      toaster.toast({ title: "Error", body: "Action failed." });
    } finally {
      setIsLoading(false);
      setStatusText("");
    }
  };

  const handleInstall = async () => {
    if (isLoading) return;
    setIsLoading(true);
    setStatusText("Installing");

    showModal(<LiveLogViewerModal logType="install" />);

    try {
      const result = (await call("install_nvidia")) as string;
      if (result.includes("ALREADY_INSTALLED")) {
        toaster.toast({ 
          title: "NVIDIA Setup", 
          body: "Drivers are already installed. No reboot required.", 
          duration: 4000 
        });
      } else if (result.includes("Failed") || result.includes("Error")) {
        toaster.toast({ 
          title: "NVIDIA Setup", 
          body: "Installation failed. Check the logs.", 
          duration: 5000 
        });
      } else {
        showModal(
          <ConfirmModal
            strTitle="Installation Complete"
            strDescription="The NVIDIA drivers were successfully installed. The system must reboot. Reboot now?"
            strOKButtonText="Restart Now"
            onOK={() => { call("reboot_system"); }}
            strCancelButtonText="Restart Later"
          />
        );
      }
    } catch (e) {
      toaster.toast({ title: "Error", body: "Plugin communication failed." });
    } finally {
      setIsLoading(false);
    }
  };

  const enableSuperGfxd = async () => {
    if (isLoading) return;
    setIsLoading(true);
    setStatusText("Restarting Daemon");
    toaster.toast({ title: "Flow Controls", body: "Restarting supergfxd..." });

    try {
      const result = await call("restart_supergfxd") as string;
      if (result === "Success") {
        toaster.toast({ title: "Flow Controls", body: "Daemon restarted successfully." });
      } else {
        toaster.toast({ title: "Error", body: result });
      }
    } catch (e) {
      toaster.toast({ title: "Error", body: "Action failed." });
    } finally {
      setIsLoading(false);
      setStatusText("");
    }
  };
  const toggleVendor = async (val: boolean) => {
    const newVendor = val ? "nvidia" : "amd";
    setSelectedVendor(newVendor);
    await call("set_setting", "gpu_vendor", newVendor ) as string;
  };
  const toggleDisplayTarget = async (val: boolean) => {
    const nextTarget = val ? "internal" : "external";
    setDisplayTarget(nextTarget);
    await call("set_setting", "display_target", nextTarget) as string;
  };
  const toggleDesktopMode = async (val: boolean) => {
    const newDesktopMode = val ? "1" : "0";
    setSelectedDesktopMode(newDesktopMode);
    await call("set_setting", "desktop_mode", newDesktopMode ) as string;
    //If they turned off the BootToDesktop, lets turn off DesktopDefault as well. 
    if (newDesktopMode === "0") {
      setSelectedDesktopDefault(newDesktopMode);
      await call("set_setting", "desktop_default", newDesktopMode ) as string;
    }
  };
  const toggleDesktopDefault = async (val: boolean) => {
    const newDesktopDefault = val ? "1" : "0";
    setSelectedDesktopDefault(newDesktopDefault);
    await call("set_setting", "desktop_default", newDesktopDefault ) as string;
  };

  const onResetClick = () => {
    showModal(
      <ConfirmModal
        strTitle="Reset Driver Environment?"
        strDescription="This will purge the NVIDIA driver stack and all filesystem redirects from SteamOS. Continue?"
        strOKButtonText="Purge & Reset"
        onOK={() => { 
          setTimeout(async () => {
            setIsLoading(true);
            showModal(<LiveLogViewerModal logType="uninstall" />);
            try {
              const result = await call("uninstall_nvidia") as string;
              if (result !== "Success") {
                toaster.toast({ title: "Reset Error", body: result });
              } else {
                showModal(
                  <ConfirmModal
                    strTitle="Reset Complete"
                    strDescription="The environment has been purged. You must reboot. Reboot now?"
                    strOKButtonText="Restart Now"
                    onOK={() => { call("reboot_system"); }}
                    strCancelButtonText="Restart Later"
                  />
                );
              }
            } catch (e) {
              toaster.toast({ title: "Error", body: "Backend unreachable." });
            } finally {
              setIsLoading(false);
            }
          }, 200);
        }}
      />
    );
  };

  // If the installation was successful, trap the user in this pure @decky/ui state
  if (needsReboot) {
    return (
      <PanelSection title="Reboot Required">
        <PanelSectionRow>
          <div style={{ marginBottom: "10px", fontSize: "14px" }}>
            The system must reboot to apply changes.
          </div>
        </PanelSectionRow>
        <PanelSectionRow>
          <ButtonItem 
            onClick={async () => {
              await call("reboot_system");
            }}
          >
            Restart Now
          </ButtonItem>
        </PanelSectionRow>
        <PanelSectionRow>
          <ButtonItem 
            onClick={() => setNeedsReboot(false)}
          >
            Restart Later
          </ButtonItem>
        </PanelSectionRow>
      </PanelSection>
    );
  }
  return (
    <Focusable>
      {/* SECTION 1: Telemetry Dashboard (Only show if connected/active) */}
      {(gpuStatus.active) && (
        <PanelSection title="Performance Monitor">
          <div style={statsStyle}>
            <div>
              <small>Temperature</small>
              <div style={{ fontSize: "1.2em", fontWeight: "bold" }}>{telemetry.temp}</div>
            </div>
            <div>
              <small>Power Draw</small>
              <div style={{ fontSize: "1.2em", fontWeight: "bold" }}>{telemetry.power}</div>
            </div>
            <div>
              <small>VRAM Usage</small>
              <div style={{ fontSize: "1.2em", fontWeight: "bold" }}>{telemetry.vram}</div>
            </div>
            <div>
              <small>GPU Load</small>
              <div style={{ fontSize: "1.2em", fontWeight: "bold" }}>{telemetry.util}</div>
            </div>
          </div>
        </PanelSection>
      )}

      {/* OS WARNING: WRONG BAZZITE IMAGE */}
      {(osType === "bazzite") && (selectedVendor === "nvidia") && (
        <PanelSection title="System Warning">
          <PanelSectionRow>
            <div style={{ color: "#ff5555", fontSize: "14px", marginBottom: "10px" }}>
              <strong>Wrong OS Image Detected!</strong><br/>
              You are running standard Bazzite. To use an NVIDIA eGPU, you MUST install the 'bazzite-deck-nvidia' image. 
              The NVIDIA XG Mobile will not function correctly on this installation. Please install the correct version or select AMD.
            </div>
          </PanelSectionRow>
        </PanelSection>
      )}

      {/* UNIVERSAL ASUS WMI CONTROLS */}
      <PanelSection title="ASUS Hardware Controls">
        <PanelSectionRow>
          <div style={{ marginBottom: "6px", fontSize: "14px", opacity: 0.8 }}>
            Quiet runs coolest. Balanced is the everyday cap. Performance allows more power. The CPU watt cap is restored only if the firmware clears it, so a boost cannot run away into a freeze. Eject and Enable once after this update.
          </div>
          <Dropdown
            selectedOption={powerProfile === "Quiet" || powerProfile === "Balanced" || powerProfile === "Performance" ? powerProfile : "Balanced"}
            rgOptions={[
              { data: "Quiet", label: "Quiet" },
              { data: "Balanced", label: "Balanced" },
              { data: "Performance", label: "Performance" }
            ]}
            onChange={async (option: any) => {
              const newProfile = option.data;
              setPowerProfile(newProfile);
              const result = await call("set_power_profile", newProfile) as string;
              toaster.toast({
                title: newProfile,
                body: String(result),
                duration: 5000
              });
              await loadLimits();
            }}
          />
        </PanelSectionRow>
      </PanelSection>

      <PanelSection title="Power and Clocks">
        {!limits && (
          <PanelSectionRow>
            <div style={{ fontSize: "13px", opacity: 0.8 }}>Reading power limits...</div>
          </PanelSectionRow>
        )}
        {limits && !limits.gpu_ready && (
          <PanelSectionRow>
            <div style={{ fontSize: "13px", opacity: 0.8 }}>
              Enable the XG Mobile to apply the eGPU limits below.
            </div>
          </PanelSectionRow>
        )}
        {limits && (
          <PanelSectionRow>
            <SliderField
              label="eGPU power limit"
              description="Lower the XG Mobile wattage to bring GPU temperature down."
              value={gpuPower}
              min={Number(limits.gpu_power_min) || 5}
              max={Number(limits.gpu_power_max) || 150}
              step={5}
              showValue
              valueSuffix=" W"
              disabled={!gpuStatus.active || isLoading}
              onChange={(value) => {
                setGpuPower(value);
                queueLimit("set_gpu_power_limit", value);
              }}
            />
          </PanelSectionRow>
        )}
        {limits && limits.gpu_kind !== "amd" && (
          <PanelSectionRow>
            <SliderField
              label="eGPU max clock"
              description="Holds the XG Mobile near this clock so frame times stay even."
              value={gpuClock}
              min={Number(limits.gpu_clock_min) || 500}
              max={Number(limits.gpu_clock_max) || 2500}
              step={50}
              showValue
              valueSuffix=" MHz"
              disabled={!gpuStatus.active || isLoading}
              onChange={(value) => {
                setGpuClock(value);
                queueLimit("set_gpu_clock_limit", value);
              }}
            />
          </PanelSectionRow>
        )}
        {limits?.cpu_tdp_supported && (
          <PanelSectionRow>
            <SliderField
              label="CPU power limit"
              description="Ends the boost at this wattage within a fraction of a second. 95°C is only a backstop if the firmware clears that cap."
              value={cpuTdp}
              min={Number(limits.cpu_tdp_min) || 8}
              max={Number(limits.cpu_tdp_max) || 45}
              step={1}
              showValue
              valueSuffix=" W"
              disabled={isLoading}
              onChange={(value) => {
                setCpuTdp(value);
                queueLimit("set_cpu_tdp", value);
              }}
            />
          </PanelSectionRow>
        )}
        {limits && !limits.cpu_tdp_supported && (
          <PanelSectionRow>
            <div style={{ fontSize: "13px", opacity: 0.8 }}>
              CPU watt control needs ryzenadj, or the ASUS power attributes in the kernel.
            </div>
          </PanelSectionRow>
        )}
        {limits?.cpu_clock_supported && (
          <PanelSectionRow>
            <SliderField
              label="CPU max clock"
              description="Caps the processor boost clock that is pushing it to 80°C."
              value={cpuClock}
              min={Number(limits.cpu_clock_min) || 1400}
              max={Number(limits.cpu_clock_max) || 5000}
              step={100}
              showValue
              valueSuffix=" MHz"
              disabled={isLoading}
              onChange={(value) => {
                setCpuClock(value);
                queueLimit("set_cpu_clock_limit", value);
              }}
            />
          </PanelSectionRow>
        )}
        <PanelSectionRow>
          <ButtonItem
            layout="below"
            disabled={isLoading}
            onClick={async () => {
              const result = await call("reset_power_limits") as string;
              toaster.toast({
                title: "Power limits",
                body: result === "Success" ? "Limits cleared" : String(result)
              });
              const prof = await call("get_power_profile") as string;
              if (prof) setPowerProfile(prof);
              await loadLimits();
            }}
          >
            Reset power limits
          </ButtonItem>
        </PanelSectionRow>
      </PanelSection>

      {/* SECTION 2: Controls */}
      <PanelSection title="Controls">
        {isLoading ? (
          <PanelSectionRow>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "center" }}>
              <Spinner />
              <span style={{ marginLeft: "10px" }}>{statusText}...</span>
            </div>
          </PanelSectionRow>
        ) : (
          <>
            <PanelSectionRow>
              <ButtonItem
                layout="below"
                disabled={!gpuStatus.connected || gpuStatus.active || isLoading}
                onClick={() => handleAction("enable_egpu", "Enabling")}
              >
                {gpuStatus.active ? "XG Mobile is Active" : "Enable XG Mobile"}
              </ButtonItem>
            </PanelSectionRow>

            <PanelSectionRow>
              <ButtonItem
                layout="below"
                disabled={!gpuStatus.active || isLoading}
                onClick={() => handleAction("eject_egpu", "Ejecting")}
              >
                {gpuStatus.active ? "Eject XG Mobile" : "XG Mobile is not Active"}
              </ButtonItem>
            </PanelSectionRow>

            {showSleepWarning && (
              <PanelSectionRow>
                <div style={{
                  backgroundColor: 'rgba(255, 0, 0, 0.15)',
                  border: '1px solid #ff4444',
                  padding: '12px',
                  borderRadius: '6px',
                  color: '#ffdddd',
                  fontSize: '13px',
                  lineHeight: '1.4'
                }}>
                  <b>⚠️ CRITICAL SLEEP WARNING</b><br/>
                  Due to NVIDIA firmware limitations on this OS, putting the device to sleep right now will cause a fatal hardware crash requiring a hard reboot. <br/><br/>
                  <b>You must Eject the eGPU before sleeping.</b>
                </div>
              </PanelSectionRow>
            )}
            <PanelSectionRow>
              <ToggleField
                label="NVIDIA Mode"
                description="Turn off for AMD XG Mobile units"
                // Disable if the hardware is active
                disabled={gpuStatus.active || isLoading}
                checked={selectedVendor === "nvidia"}
                onChange={toggleVendor}
              />
            </PanelSectionRow>
            <PanelSectionRow>
              <ToggleField
                label="Play on Internal Screen"
                description="Keep Game Mode on the Ally's built-in display. Games render on the XG Mobile and the picture is sent back to the internal screen. Turn this off to use a monitor plugged into the XG Mobile."
                disabled={gpuStatus.active || isLoading}
                checked={displayTarget === "internal"}
                onChange={toggleDisplayTarget}
              />
            </PanelSectionRow>
            <PanelSectionRow>
              <ToggleField
                label="DesktopMode on Enable"
                description="Turn on to reboot straight into Desktop Mode when running the Enable. Recommended for 4K and Ultrawide resolutions with NVIDIA GPUs."
                // Disable if the hardware is active
                disabled={gpuStatus.active || isLoading}
                checked={selectedDesktopMode === "1"}
                onChange={toggleDesktopMode}
              />
            </PanelSectionRow>
            <PanelSectionRow>
              <ToggleField
                label="Set Desktop to Default"
                description="Turn on to set Desktop Mode to Default when running the XG Mobile. You MUST run the Eject Desktop Link to revert to GameMode."
                // Disable if the hardware is active
                disabled={gpuStatus.active || isLoading || selectedDesktopMode === "0"}
                checked={selectedDesktopDefault === "1"}
                onChange={toggleDesktopDefault}
              />
            </PanelSectionRow>
          </>
        )}
      </PanelSection>

      {/* SECTION 3: Maintenance */}
      <PanelSection title="Advanced">
        <PanelSectionRow>
          <ButtonItem
            layout="below"
            description="Create Desktop shortcuts for enabling and ejecting the XG Mobile. Run this after selecting Vender type and running Install NVIDIA Drivers if needed."
            disabled={isLoading}
            onClick={() => handleAction("create_desktop_shortcuts", "Creating Shortcuts")}
          >
            Create Desktop Shortcuts
          </ButtonItem>
        </PanelSectionRow>
        {/* Render Install button ONLY on SteamOS */}
        {osType === "steamos" && (
          <PanelSectionRow>
            <ButtonItem
              layout="below"
              disabled={gpuStatus.active || isLoading}
              onClick={handleInstall} 
            >
              Install NVIDIA Drivers
            </ButtonItem>
          </PanelSectionRow>
        )}
        {/* Render Reset button ONLY on SteamOS */}
        {osType === "steamos" && (
          <PanelSectionRow>
            <ButtonItem
              layout="below"
              disabled={gpuStatus.active || isLoading}
              onClick={onResetClick}
            >
              <span style={{ color: "#ff5555" }}>Reset Driver Environment</span>
            </ButtonItem>
          </PanelSectionRow>
        )}

        {/* SUPERGFXCTL CONTROLS */}
        {deviceType === "laptop" && (//{hasSupergfxctl && (
          <PanelSection title="Flow Laptop Controls">
            <PanelSectionRow>
              <ButtonItem layout="below" onClick={() => enableSuperGfxd()}>
                Restart Supergfxd
              </ButtonItem>
              <ButtonItem
                layout="below"
                disabled={isLoading}
                onClick={() => handleAction("hybrid_supergfxctl", "Switching to Hybrid")}
              >
                Send Supergfxctl Hybrid command - beta
              </ButtonItem>
            </PanelSectionRow>

            <PanelSectionRow>
              <ButtonItem
                layout="below"
                disabled={isLoading}
                onClick={() => handleAction("integrated_supergfxctl", "Switching to Integrated")}
              >
                Send Supergfxctl Integrated command - beta
              </ButtonItem>
            </PanelSectionRow>
          </PanelSection>
        )}

        {/* Universal Debug Tools */}
        <PanelSectionRow>
          <ButtonItem
            layout="below"
            onClick={() => showModal(<LogViewerModal />)} 
          >
            View Activity Logs
          </ButtonItem>
        </PanelSectionRow>
      </PanelSection>

      <PanelSection title="About">
        <PanelSectionRow>
          <div style={{ display: "flex", justifyContent: "space-between", opacity: 0.6, fontSize: "0.8em" }}>
            <span>Version</span>
            <span>{pluginVersion}</span>
          </div>
        </PanelSectionRow>
        <PanelSectionRow>
          <div style={{ opacity: 0.6, fontSize: "0.8em" }}>
            Original plugin by Kenny Hood (Kentronix). This build adds internal screen, power limits, and frame pacing.
          </div>
        </PanelSectionRow>
        <PanelSectionRow>
          <div style={{ opacity: 0.6, fontSize: "0.8em" }}>
            Changes by tanayabhay-creator. GPL-3.
          </div>
        </PanelSectionRow>
      </PanelSection>
    </Focusable>
  );
};
