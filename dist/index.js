var DefaultContext = {
  color: undefined,
  size: undefined,
  className: undefined,
  style: undefined,
  attr: undefined
};
var IconContext = SP_REACT.createContext && SP_REACT.createContext(DefaultContext);

var __assign = window && window.__assign || function () {
  __assign = Object.assign || function (t) {
    for (var s, i = 1, n = arguments.length; i < n; i++) {
      s = arguments[i];
      for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p)) t[p] = s[p];
    }
    return t;
  };
  return __assign.apply(this, arguments);
};
var __rest = window && window.__rest || function (s, e) {
  var t = {};
  for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0) t[p] = s[p];
  if (s != null && typeof Object.getOwnPropertySymbols === "function") for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
    if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i])) t[p[i]] = s[p[i]];
  }
  return t;
};
function Tree2Element(tree) {
  return tree && tree.map(function (node, i) {
    return SP_REACT.createElement(node.tag, __assign({
      key: i
    }, node.attr), Tree2Element(node.child));
  });
}
function GenIcon(data) {
  // eslint-disable-next-line react/display-name
  return function (props) {
    return SP_REACT.createElement(IconBase, __assign({
      attr: __assign({}, data.attr)
    }, props), Tree2Element(data.child));
  };
}
function IconBase(props) {
  var elem = function (conf) {
    var attr = props.attr,
      size = props.size,
      title = props.title,
      svgProps = __rest(props, ["attr", "size", "title"]);
    var computedSize = size || conf.size || "1em";
    var className;
    if (conf.className) className = conf.className;
    if (props.className) className = (className ? className + " " : "") + props.className;
    return SP_REACT.createElement("svg", __assign({
      stroke: "currentColor",
      fill: "currentColor",
      strokeWidth: "0"
    }, conf.attr, attr, svgProps, {
      className: className,
      style: __assign(__assign({
        color: props.color || conf.color
      }, conf.style), props.style),
      height: computedSize,
      width: computedSize,
      xmlns: "http://www.w3.org/2000/svg"
    }), title && SP_REACT.createElement("title", null, title), props.children);
  };
  return IconContext !== undefined ? SP_REACT.createElement(IconContext.Consumer, null, function (conf) {
    return elem(conf);
  }) : elem(DefaultContext);
}

// THIS FILE IS AUTO GENERATED
function BsGpuCard (props) {
  return GenIcon({"tag":"svg","attr":{"fill":"currentColor","viewBox":"0 0 16 16"},"child":[{"tag":"path","attr":{"d":"M4 8a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0Zm7.5-1.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Z"}},{"tag":"path","attr":{"d":"M0 1.5A.5.5 0 0 1 .5 1h1a.5.5 0 0 1 .5.5V4h13.5a.5.5 0 0 1 .5.5v7a.5.5 0 0 1-.5.5H2v2.5a.5.5 0 0 1-1 0V2H.5a.5.5 0 0 1-.5-.5Zm5.5 4a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5ZM9 8a2.5 2.5 0 1 0 5 0 2.5 2.5 0 0 0-5 0Z"}},{"tag":"path","attr":{"d":"M3 12.5h3.5v1a.5.5 0 0 1-.5.5H3.5a.5.5 0 0 1-.5-.5v-1Zm4 1v-1h4v1a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5Z"}}]})(props);
}

const manifest = {"name":"XGMobile-Manager","author":"Kenny Hood (Kentronix); changes by tanayabhay-creator","flags":["debug","root"],"api_version":2,"main":"dist/index.js","bin":"python3","python_src":"main.py","publish":{"tags":["eGPU","NVIDIA","AMD","XGMobile","ROG-Ally","Flow","ASUS"],"description":"Derivative of Kenny Hood (Kentronix)'s XG Mobile Manager. Adds internal-screen Game Mode and frame pacing."}};
const API_VERSION = 2;
if (!manifest?.name) {
    throw new Error('[@decky/api]: Failed to find plugin manifest.');
}
const internalAPIConnection = window.__DECKY_SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED_deckyLoaderAPIInit;
if (!internalAPIConnection) {
    throw new Error('[@decky/api]: Failed to connect to the loader as as the loader API was not initialized. This is likely a bug in Decky Loader.');
}
let api;
try {
    api = internalAPIConnection.connect(API_VERSION, manifest.name);
}
catch {
    api = internalAPIConnection.connect(1, manifest.name);
    console.warn(`[@decky/api] Requested API version ${API_VERSION} but the running loader only supports version 1. Some features may not work.`);
}
if (api._version != API_VERSION) {
    console.warn(`[@decky/api] Requested API version ${API_VERSION} but the running loader only supports version ${api._version}. Some features may not work.`);
}
const call = api.call;
const toaster = api.toaster;
const definePlugin = (fn) => {
    return (...args) => {
        return fn(...args);
    };
};

/**
 * Class for frontend -> backend communication.
 */
class PythonInterop {
    /**
     * Logs a message to the plugin's log file and the frontend console.
     * @param message The message to log.
     */
    static async log(message) {
        await call("logMessage", `[front-end]: ${message}`, 0);
    }
    /**
     * Logs a warning to the plugin's log file and the frontend console.
     * @param message The message to log.
     */
    static async warn(message) {
        await call("logMessage", `[front-end]: ${message}`, 1);
    }
    /**
     * Logs an error to the plugin's log file and the frontend console.
     * @param message The message to log.
     */
    static async error(message) {
        await call("logMessage", `[front-end]: ${message}`, 2);
    }
    /**
     * Sets the plugin's demo settings.
     * @param demo The plugin's demo settings.
     * @returns A promise resolving to whether or not the demo settings were successfully set.
     */
    static async setDemo(demo) {
        try {
            return await call("set_demo", demo);
        }
        catch (e) {
            return e;
        }
    }
    /**
     * Shows a toast message.
     * @param title The title of the toast.
     * @param message The message of the toast.
     */
    /*static toast(title: string, message: string): void {
      return (() => {
        try {
          return toaster.toast({
            title: title,
            body: message,
            duration: 8000,
          });
        } catch (e) {
          console.log("Toaster Error", e);
        }
      }*/
    static toast(title, message) {
        // using 'any' or 'ToastNotification | undefined'
        try {
            return toaster.toast({
                title: title,
                body: message,
                duration: 8000,
            });
        }
        catch (e) {
            console.log("Toaster Error", e);
        } //)();
    }
}

/**
 * Waits for a condition to be true.
 * @param retries The number of times to retry the condition.
 * @param delay The time (in ms) between retdecky-frontend-libries.
 * @param check The condition to check.
 * @returns A promise resolving to true if the check was true on any attempt, or false if it failed each time.
 */
async function waitForCondition(retries, delay, check) {
    const waitImpl = async () => {
        try {
            let tries = retries + 1;
            while (tries-- !== 0) {
                if (await check()) {
                    return true;
                }
                if (tries > 0) {
                    await DFL.sleep(delay);
                }
            }
        }
        catch (error) {
            console.error(error);
        }
        return false;
    };
    return await waitImpl();
}
function debounce(func, wait, immediate) {
    let timeout;
    return function () {
        const context = this, args = arguments;
        const later = function () {
            timeout = null;
            if (!immediate)
                func.apply(context, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow)
            func.apply(context, args);
    };
}

const errorToastLimit = 10;
/**
 * The logging controller for the Plugin.
 */
class LogController {
    /**
     * Sets up the plugin logger.
     * @param pluginName The name of the plugin (in UpperCamelCase).
     * @param pluginColor The main color of the plugin.
     * @param infoColor The color for info messages. Defaults to **1abc9c**
     * @param warningColor The color for warning messages. Defaults to **e3c907**
     * @param errorColor The color for error messages. Defaults to **c70808**
     */
    static setup(pluginName, pluginColor, infoColor = "1abc9c", warningColor = "e3c907", errorColor = "c70808") {
        LogController.pluginName = pluginName;
        LogController.pluginColor = pluginColor;
        LogController.infoColor = infoColor;
        LogController.warningColor = warningColor;
        LogController.errorColor = errorColor;
    }
    /**
     * Logs a message to the plugin's log file and the frontend console.
     * @param message The message to log.
     */
    static log(...args) {
        console.log(`%c ${LogController.pluginName} %c INFO %c`, `background: #${LogController.pluginColor}; color: black;`, `background: #${LogController.infoColor}; color: black;`, 'background: transparent;', ...args);
        PythonInterop.log(args.join(" "));
    }
    /**
     * Logs a warning to the plugin's log file and the frontend console.
     */
    static warn(...args) {
        console.warn(`%c ${LogController.pluginName} %c WARNING %c`, `background: #${LogController.pluginColor}; color: black;`, `background: #${LogController.warningColor}; color: black;`, 'background: transparent;', ...args);
        PythonInterop.warn(args.join(" "));
    }
    /**
     * Logs an error to the plugin's log file and the frontend console.
     */
    static error(...args) {
        console.error(`%c ${LogController.pluginName} %c ERROR %c`, `background: #${LogController.pluginColor}; color: black;`, `background: #${LogController.errorColor}; color: black;`, 'background: transparent;', ...args);
        PythonInterop.error(args.join(" "));
    }
    /**
     * Throws a new error and logs it to the plugin's log file.
     */
    static throw(...args) {
        PythonInterop.error(args.join(" "));
        throw new Error([
            `%c ${LogController.pluginName} %c ERROR %c`,
            `background: #${LogController.pluginColor}; color: black;`,
            `background: #${LogController.errorColor}; color: black;`,
            'background: transparent;',
            ...args
        ].join(' '));
    }
}
/**
 * Error flag to check for showing a problem has occured in the QAM.
 */
LogController.errorFlag = false;
/**
 * Counts of raised error massages
 */
LogController.errorCounts = {};
/**
 * Logs error to backend, frontend, and toasts the error and sets the error flag to show in QAM.
 *
 * intended for patching/ ui errors but may be useful for other cases in the future.
 */
LogController.raiseError = debounce((...args) => {
    const msg = args.join(" ");
    PythonInterop.error(msg);
    LogController.error(...args);
    if (!LogController.errorCounts[msg])
        LogController.errorCounts[msg] = 0;
    if (LogController.errorCounts[msg] <= errorToastLimit)
        PythonInterop.toast(`${LogController.pluginName} ERROR`, msg);
    LogController.errorCounts[msg]++;
    LogController.errorFlag = true;
}, 5000, true);

/**
 * Wrapper class for the SteamClient interface.
 */
class SteamController {
    constructor() {
        this.hasLoggedIn = false;
        this.hasLoggedOut = false;
    }
    // TODO: put wrappers for SteamClient methods here.
    /**
     * Registers a hook for when the user's login state changes.
     * @param onLogin Function to run on login.
     * @param onLogout Function to run on logout.
     * @param once Whether the hook should run once.
     * @param waitForPasscode Whether the hook should only run once the passcode has been entered.
     * @returns A function to unregister the hook.
     */
    registerForAuthStateChange(onLogin, onLogout, once, waitForPasscode) {
        try {
            let isLoggedIn = null;
            const currentUsername = loginStore.m_strAccountName;
            return SteamClient.User.RegisterForLoginStateChange((username) => {
                if (username === "") {
                    if (isLoggedIn !== false && (once ? !this.hasLoggedOut : true)) {
                        if (onLogout)
                            onLogout(currentUsername);
                    }
                    isLoggedIn = false;
                }
                else {
                    if (isLoggedIn !== true && (once ? !this.hasLoggedIn : true)) {
                        if (onLogin) {
                            if (waitForPasscode && securitystore.IsLockScreenActive()) {
                                waitForCondition(100, 250, () => !securitystore.IsLockScreenActive()).then(() => {
                                    //* basically, wait up to 25 minutes for the user to enter their passcode, and at that point, if they have logged in, initialize regardless.
                                    onLogin(username);
                                });
                            }
                            else {
                                onLogin(username);
                            }
                        }
                    }
                    isLoggedIn = true;
                }
            });
        }
        catch (error) {
            LogController.log(`error with AuthStateChange hook. [DEBUG INFO] error: ${error};`);
            // @ts-ignore
            return () => { };
        }
    }
    /**
     * Waits until the services are initialized.
     * @returns A promise resolving to true if services were initialized on any attempt, or false if all attemps failed.
     */
    async waitForServicesToInitialize() {
        const servicesFound = await waitForCondition(20, 250, () => window.App?.WaitForServicesInitialized != null);
        if (servicesFound) {
            LogController.log(`Services found.`);
        }
        else {
            LogController.log(`Couldn't find services.`);
        }
        return (await window.App?.WaitForServicesInitialized?.().then((success) => {
            LogController.log(`Services initialized. Success: ${success}`);
            return success;
        })) ?? false;
    }
}

/**
 * Main controller class for the plugin.
 */
class PluginController {
    /**
     * Sets the plugin's serverAPI.
     * @param pluginState The plugin's state management.
     */
    static setup(pluginState) {
        LogController.setup("xgmobile-manager", "ff3e00");
        this.pluginState = pluginState;
        this.steamController = new SteamController();
    }
    /**
     * Sets the plugin to initialize once the user logs in.
     * @returns The unregister function for the login hook.
     */
    static initOnLogin(onMount) {
        return this.steamController.registerForAuthStateChange(
        // * This function will get passed the current user's username as its first argument.
        async () => {
            if (await this.steamController.waitForServicesToInitialize()) {
                await PluginController.init();
                onMount();
            }
            else {
                PythonInterop.toast("Error", "Failed to initialize, try restarting.");
            }
        }, 
        // * This function will get passed the current user's username as its first argument.
        async () => { }, true, true);
    }
    /**
     * Initializes the Plugin.
     */
    static async init() {
        LogController.log("PluginController initialized.");
        // TODO: perform any logic you want to run when the plugin first initializes here.
    }
    /**
     * Function to run when the plugin dismounts.
     */
    static dismount() {
        // TODO: dispose of any listeners and perform other cleanup here.
        LogController.log("PluginController dismounted.");
    }
}

const PluginContext = SP_REACT.createContext(null);
const PluginContextProvider = ({ children, PluginStateClass }) => {
    const [publicState, setPublicState] = SP_REACT.useState({
        ...PluginStateClass.getPublicState()
    });
    SP_REACT.useEffect(() => {
        const onUpdate = () => {
            setPublicState({ ...PluginStateClass.getPublicState() });
        };
        PluginStateClass.eventBus.addEventListener("stateUpdate", onUpdate);
        return () => {
            PluginStateClass.eventBus.removeEventListener("stateUpdate", onUpdate);
        };
    }, [PluginStateClass]);
    // Setter wrappers
    const setButtonLabel = (label) => {
        PluginStateClass.setButtonLabel(label);
    };
    return (SP_JSX.jsx(PluginContext.Provider, { value: {
            ...publicState,
            setButtonLabel
        }, children: children }));
};

// * Define your property defaults and setter logic here.
class PluginState {
    constructor() {
        this.buttonLabel = "Click Me";
        this.eventBus = new EventTarget();
    }
    getPublicState() {
        return {
            "buttonLabel": this.buttonLabel
        };
    }
    setButtonLabel(label) {
        this.buttonLabel = label;
        this.forceUpdate();
    }
    forceUpdate() {
        this.eventBus.dispatchEvent(new Event("stateUpdate"));
    }
}

const LiveLogViewerModal = ({ closeModal, logType, title }) => {
    const [logText, setLogText] = SP_REACT.useState("Initializing...");
    const scrollRef = SP_REACT.useRef(null);
    SP_REACT.useEffect(() => {
        const interval = setInterval(async () => {
            try {
                const logs = await call("get_live_logs", logType);
                if (logs)
                    setLogText(logs);
            }
            catch (e) {
                console.error("Polling error:", e);
            }
        }, 500);
        return () => clearInterval(interval);
    }, [logType]);
    SP_REACT.useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
        }
    }, [logText]);
    return (SP_JSX.jsxs(DFL.ModalRoot, { onCancel: closeModal, children: [SP_JSX.jsx(DFL.DialogHeader, { children: title || "XG Mobile Maintenance" }), SP_JSX.jsx(DFL.DialogBody, { children: SP_JSX.jsx(DFL.Focusable, { children: SP_JSX.jsx("div", { ref: scrollRef, style: {
                            backgroundColor: "#000000",
                            color: "#1df51d",
                            padding: "12px",
                            fontFamily: "monospace",
                            height: "350px",
                            overflowY: "auto",
                            fontSize: "12px",
                            borderRadius: "4px",
                            whiteSpace: "pre-wrap"
                        }, children: logText }) }) })] }));
};

const LOG_OPTIONS = [
    { data: 'enable', label: 'Enable Log' },
    { data: 'enabledesktop', label: 'Enable Desktop Link Log' },
    { data: 'eject', label: 'Eject Log' },
    { data: 'ejectdesktop', label: 'Eject Desktop Link Log' },
    { data: 'transition', label: 'Desktop Transition Log' },
    { data: 'hybrid', label: 'Supergfx Hybrid Log' },
    { data: 'integrated', label: 'Supergfx Integrated Log' },
    { data: 'install', label: 'NVIDIA Install Log' },
    //{ data: 'repair', label: 'Repair Services Log' },
    { data: 'uninstall', label: 'Reset Environment Log' },
    { data: 'shortcuts', label: 'Install Desktop Shortcuts Log' },
    { data: 'sync', label: 'Boot NVIDIA DRM sync Log' },
    { data: 'python', label: 'Python Critical Error Log' },
    { data: 'debug', label: 'Debug Log' }
];
const LogViewerModal = ({ closeModal }) => {
    const [logType, setSelectedLog] = SP_REACT.useState('enable');
    const [logText, setLogText] = SP_REACT.useState("Reading log...");
    SP_REACT.useEffect(() => {
        const fetchLog = async () => {
            try {
                const logs = await call("get_latest_logs", logType);
                setLogText(logs || `No logs found for ${logType}.`);
            }
            catch (e) {
                setLogText("Backend connection failed.");
            }
        };
        fetchLog();
    }, [logType]);
    return (SP_JSX.jsxs(DFL.ModalRoot, { onCancel: closeModal, children: [SP_JSX.jsx(DFL.DialogHeader, { children: "Activity Logs" }), SP_JSX.jsx(DFL.DialogBody, { children: SP_JSX.jsxs("div", { style: { display: 'flex', flexDirection: 'column', gap: '15px' }, children: [SP_JSX.jsx(DFL.Dropdown, { rgOptions: LOG_OPTIONS, selectedOption: logType, onChange: (option) => setSelectedLog(option.data) }), SP_JSX.jsx("div", { style: { height: '400px', backgroundColor: '#000000', borderRadius: '4px' }, children: SP_JSX.jsx(DFL.ScrollPanel, { children: SP_JSX.jsx("div", { style: {
                                        padding: '10px' /*, Dunno if I want this green or not
                                        fontFamily: 'monospace',
                                        whiteSpace: 'pre-wrap',
                                        fontSize: '12px',
                                        color: '#1df51d'*/
                                    }, children: logText }) }) })] }) })] }));
};

// Helper for UI styling
const statsStyle = {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: "4px",
    padding: "6px",
    backgroundColor: "rgba(0,0,0,0.2)",
    borderRadius: "4px",
    marginBottom: "10px"
};
const QuickAccessContent = () => {
    const [needsReboot, setNeedsReboot] = SP_REACT.useState(false);
    const [isLoading, setIsLoading] = SP_REACT.useState(false);
    const [statusText, setStatusText] = SP_REACT.useState("");
    const [pluginVersion, setPluginVersion] = SP_REACT.useState("Loading...");
    //const [hasSupergfxctl, setHasSupergfxctl] = useState<boolean>(false);
    const [deviceType, setDeviceType] = SP_REACT.useState("unknown");
    //const [daemonActive, setDaemonActive] = useState<boolean>(true);
    // Dynamic State from Backend
    const [gpuStatus, setGpuStatus] = SP_REACT.useState({ connected: false, active: false, vendor: "none" });
    const [telemetry, setTelemetry] = SP_REACT.useState({ temp: "--", power: "--", vram: "--", util: "--" });
    const [selectedVendor, setSelectedVendor] = SP_REACT.useState("nvidia");
    const [selectedDesktopMode, setSelectedDesktopMode] = SP_REACT.useState("0");
    const [selectedDesktopDefault, setSelectedDesktopDefault] = SP_REACT.useState("0");
    const [displayTarget, setDisplayTarget] = SP_REACT.useState("external");
    const [panel60, setPanel60] = SP_REACT.useState(false);
    const [osType, setOsType] = SP_REACT.useState("steamos");
    const [powerProfile, setPowerProfile] = SP_REACT.useState("Unknown");
    const [gpuWatts, setGpuWatts] = SP_REACT.useState(91);
    const [gpuClock, setGpuClock] = SP_REACT.useState(1100);
    const [cpuWatts, setCpuWatts] = SP_REACT.useState(15);
    const [cpuClock, setCpuClock] = SP_REACT.useState(3300);
    const showSleepWarning = gpuStatus.active && selectedVendor === 'nvidia' && (osType.includes('bazzite') || osType === 'cachyos');
    // 1. Initial Load: Fetch Vendor Setting once
    SP_REACT.useEffect(() => {
        const init = async () => {
            try {
                const val = await call("get_setting", "gpu_vendor", "nvidia");
                setSelectedVendor(val);
                const desktopmode = await call("get_setting", "desktop_mode", "0");
                setSelectedDesktopMode(desktopmode);
                const desktopdefault = await call("get_setting", "desktop_default", "0");
                setSelectedDesktopDefault(desktopdefault);
                const display = await call("get_setting", "display_target", "external");
                setDisplayTarget(display === "internal" ? "internal" : "external");
                const panel = await call("get_setting", "panel_60hz", "0");
                setPanel60(panel === "1");
                const ver = await call("get_version");
                if (ver)
                    setPluginVersion(ver);
                const os = await call("get_os_status");
                setOsType(os);
                // call() returns the boolean directly from Python
                //const hasSgfx = await call("has_supergfxctl") as boolean;
                //setHasSupergfxctl(hasSgfx);
                const device = await call("get_device_type");
                setDeviceType(device);
            }
            catch (e) {
                console.error("Init Error:", e);
            }
        };
        init();
    }, []);
    // 2. The Polling Loop: Only fetches hardware/telemetry
    SP_REACT.useEffect(() => {
        const poll = async () => {
            try {
                const currentStatus = await call("get_gpu_status");
                if (currentStatus)
                    setGpuStatus(currentStatus);
                if (currentStatus?.active) {
                    const stats = await call("get_telemetry");
                    if (stats)
                        setTelemetry(stats);
                }
                const prof = await call("get_power_profile");
                setPowerProfile(prof);
            }
            catch (e) {
                console.error("Poll Error:", e);
            }
        };
        poll();
        const interval = setInterval(poll, 3000);
        return () => clearInterval(interval);
    }, []);
    SP_REACT.useEffect(() => {
        if (powerProfile !== "Quiet" && powerProfile !== "Balanced" && powerProfile !== "Performance")
            return;
        let cancelled = false;
        const loadProfileValues = async () => {
            try {
                const values = await call("get_profile_values", { profile: powerProfile });
                if (cancelled || !values)
                    return;
                setGpuWatts(Number(values.gpu_watts) || 0);
                setGpuClock(Number(values.gpu_clock) || 0);
                setCpuWatts(Number(values.cpu_watts) || 0);
                setCpuClock(Number(values.cpu_clock) || 0);
            }
            catch (e) {
                console.error("Profile value error:", e);
            }
        };
        loadProfileValues();
        return () => { cancelled = true; };
    }, [powerProfile]);
    const setProfileValues = async () => {
        const result = await call("set_profile_values", {
            profile: powerProfile,
            gpu_watts: Math.round(gpuWatts),
            gpu_clock: Math.round(gpuClock),
            cpu_watts: Math.round(cpuWatts),
            cpu_clock: Math.round(cpuClock),
        });
        toaster.toast({
            title: powerProfile,
            body: result === "Success" ? "Values set once." : String(result),
            duration: 5000,
        });
    };
    const handleAction = async (method, label) => {
        if (isLoading)
            return;
        setIsLoading(true);
        setStatusText(label);
        toaster.toast({ title: "XG Mobile", body: `Starting: ${label}...` });
        try {
            if (selectedDesktopMode === "1" && method === "enable_egpu") {
                //toaster.toast({ title: "Debug", body: "1. React is calling Python..." });
                const flagResult = await call("set_desktop_transition_flag");
                toaster.toast({ title: "DesktopMode", body: `Setting Desktop Flag: ${flagResult}` });
                //await new Promise(resolve => setTimeout(resolve, 3000));
            }
            const result = await call(method);
            toaster.toast({ title: "XG Mobile", body: `${label} Result: ${result}` });
            const newStatus = await call("get_gpu_status");
            setGpuStatus(newStatus);
        }
        catch (e) {
            toaster.toast({ title: "Error", body: "Action failed." });
        }
        finally {
            setIsLoading(false);
            setStatusText("");
        }
    };
    const handleInstall = async () => {
        if (isLoading)
            return;
        setIsLoading(true);
        setStatusText("Installing");
        DFL.showModal(SP_JSX.jsx(LiveLogViewerModal, { logType: "install" }));
        try {
            const result = (await call("install_nvidia"));
            if (result.includes("ALREADY_INSTALLED")) {
                toaster.toast({
                    title: "NVIDIA Setup",
                    body: "Drivers are already installed. No reboot required.",
                    duration: 4000
                });
            }
            else if (result.includes("Failed") || result.includes("Error")) {
                toaster.toast({
                    title: "NVIDIA Setup",
                    body: "Installation failed. Check the logs.",
                    duration: 5000
                });
            }
            else {
                DFL.showModal(SP_JSX.jsx(DFL.ConfirmModal, { strTitle: "Installation Complete", strDescription: "The NVIDIA drivers were successfully installed. The system must reboot. Reboot now?", strOKButtonText: "Restart Now", onOK: () => { call("reboot_system"); }, strCancelButtonText: "Restart Later" }));
            }
        }
        catch (e) {
            toaster.toast({ title: "Error", body: "Plugin communication failed." });
        }
        finally {
            setIsLoading(false);
        }
    };
    const enableSuperGfxd = async () => {
        if (isLoading)
            return;
        setIsLoading(true);
        setStatusText("Restarting Daemon");
        toaster.toast({ title: "Flow Controls", body: "Restarting supergfxd..." });
        try {
            const result = await call("restart_supergfxd");
            if (result === "Success") {
                toaster.toast({ title: "Flow Controls", body: "Daemon restarted successfully." });
            }
            else {
                toaster.toast({ title: "Error", body: result });
            }
        }
        catch (e) {
            toaster.toast({ title: "Error", body: "Action failed." });
        }
        finally {
            setIsLoading(false);
            setStatusText("");
        }
    };
    const toggleVendor = async (val) => {
        const newVendor = val ? "nvidia" : "amd";
        setSelectedVendor(newVendor);
        await call("set_setting", "gpu_vendor", newVendor);
    };
    const toggleDisplayTarget = async (val) => {
        const nextTarget = val ? "internal" : "external";
        setDisplayTarget(nextTarget);
        await call("set_setting", "display_target", nextTarget);
    };
    const togglePanel60 = async (val) => {
        setPanel60(val);
        await call("set_setting", "panel_60hz", val ? "1" : "0");
    };
    const toggleDesktopMode = async (val) => {
        const newDesktopMode = val ? "1" : "0";
        setSelectedDesktopMode(newDesktopMode);
        await call("set_setting", "desktop_mode", newDesktopMode);
        //If they turned off the BootToDesktop, lets turn off DesktopDefault as well. 
        if (newDesktopMode === "0") {
            setSelectedDesktopDefault(newDesktopMode);
            await call("set_setting", "desktop_default", newDesktopMode);
        }
    };
    const toggleDesktopDefault = async (val) => {
        const newDesktopDefault = val ? "1" : "0";
        setSelectedDesktopDefault(newDesktopDefault);
        await call("set_setting", "desktop_default", newDesktopDefault);
    };
    const onResetClick = () => {
        DFL.showModal(SP_JSX.jsx(DFL.ConfirmModal, { strTitle: "Reset Driver Environment?", strDescription: "This will purge the NVIDIA driver stack and all filesystem redirects from SteamOS. Continue?", strOKButtonText: "Purge & Reset", onOK: () => {
                setTimeout(async () => {
                    setIsLoading(true);
                    DFL.showModal(SP_JSX.jsx(LiveLogViewerModal, { logType: "uninstall" }));
                    try {
                        const result = await call("uninstall_nvidia");
                        if (result !== "Success") {
                            toaster.toast({ title: "Reset Error", body: result });
                        }
                        else {
                            DFL.showModal(SP_JSX.jsx(DFL.ConfirmModal, { strTitle: "Reset Complete", strDescription: "The environment has been purged. You must reboot. Reboot now?", strOKButtonText: "Restart Now", onOK: () => { call("reboot_system"); }, strCancelButtonText: "Restart Later" }));
                        }
                    }
                    catch (e) {
                        toaster.toast({ title: "Error", body: "Backend unreachable." });
                    }
                    finally {
                        setIsLoading(false);
                    }
                }, 200);
            } }));
    };
    // If the installation was successful, trap the user in this pure @decky/ui state
    if (needsReboot) {
        return (SP_JSX.jsxs(DFL.PanelSection, { title: "Reboot Required", children: [SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx("div", { style: { marginBottom: "10px", fontSize: "14px" }, children: "The system must reboot to apply changes." }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { onClick: async () => {
                            await call("reboot_system");
                        }, children: "Restart Now" }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { onClick: () => setNeedsReboot(false), children: "Restart Later" }) })] }));
    }
    return (SP_JSX.jsxs(DFL.Focusable, { children: [(gpuStatus.active) && (SP_JSX.jsx(DFL.PanelSection, { title: "Performance Monitor", children: SP_JSX.jsxs("div", { style: statsStyle, children: [SP_JSX.jsxs("div", { children: [SP_JSX.jsx("small", { children: "Temperature" }), SP_JSX.jsx("div", { style: { fontSize: "1.2em", fontWeight: "bold" }, children: telemetry.temp })] }), SP_JSX.jsxs("div", { children: [SP_JSX.jsx("small", { children: "Power Draw" }), SP_JSX.jsx("div", { style: { fontSize: "1.2em", fontWeight: "bold" }, children: telemetry.power })] }), SP_JSX.jsxs("div", { children: [SP_JSX.jsx("small", { children: "VRAM Usage" }), SP_JSX.jsx("div", { style: { fontSize: "1.2em", fontWeight: "bold" }, children: telemetry.vram })] }), SP_JSX.jsxs("div", { children: [SP_JSX.jsx("small", { children: "GPU Load" }), SP_JSX.jsx("div", { style: { fontSize: "1.2em", fontWeight: "bold" }, children: telemetry.util })] })] }) })), (osType === "bazzite") && (selectedVendor === "nvidia") && (SP_JSX.jsx(DFL.PanelSection, { title: "System Warning", children: SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsxs("div", { style: { color: "#ff5555", fontSize: "14px", marginBottom: "10px" }, children: [SP_JSX.jsx("strong", { children: "Wrong OS Image Detected!" }), SP_JSX.jsx("br", {}), "You are running standard Bazzite. To use an NVIDIA eGPU, you MUST install the 'bazzite-deck-nvidia' image. The NVIDIA XG Mobile will not function correctly on this installation. Please install the correct version or select AMD."] }) }) })), SP_JSX.jsx(DFL.PanelSection, { title: "ASUS Hardware Controls", children: SP_JSX.jsxs(DFL.PanelSectionRow, { children: [SP_JSX.jsx("div", { style: { marginBottom: "6px", fontSize: "14px", opacity: 0.8 }, children: "Active Power Profile" }), powerProfile === "Error" || powerProfile === "Unknown" ? (SP_JSX.jsx("div", { style: { color: "#ffab40", fontSize: "12px", fontStyle: "italic", padding: "4px 0" }, children: "Unable to read motherboard WMI policy." })) : (SP_JSX.jsx(DFL.Dropdown, { selectedOption: powerProfile, rgOptions: [
                                { data: "Quiet", label: "Quiet" },
                                { data: "Balanced", label: "Balanced" },
                                { data: "Performance", label: "Performance" }
                            ], onChange: async (option) => {
                                const newProfile = option.data;
                                setPowerProfile(newProfile);
                                await call("set_power_profile", { profile: newProfile });
                                toaster.toast({ title: "ASUS Profile", body: `Set to ${newProfile}` });
                            } })), SP_JSX.jsx("div", { style: { marginTop: "8px", fontSize: "12px", opacity: 0.7 }, children: "These values belong to the selected profile. Set writes them once. Nothing keeps rewriting them." }), powerProfile === "Quiet" || powerProfile === "Balanced" || powerProfile === "Performance" ? (SP_JSX.jsxs(SP_JSX.Fragment, { children: [SP_JSX.jsx(DFL.SliderField, { label: "eGPU watts", value: gpuWatts, min: 5, max: 150, step: 1, onChange: setGpuWatts }), SP_JSX.jsx(DFL.SliderField, { label: "eGPU max clock", value: gpuClock, min: 300, max: 2100, step: 50, onChange: setGpuClock }), SP_JSX.jsx(DFL.SliderField, { label: "CPU watts", value: cpuWatts, min: 8, max: 30, step: 1, onChange: setCpuWatts }), SP_JSX.jsx(DFL.SliderField, { label: "CPU max clock", value: cpuClock, min: 1400, max: 5000, step: 100, onChange: setCpuClock }), SP_JSX.jsxs(DFL.ButtonItem, { layout: "below", onClick: setProfileValues, children: ["Set ", powerProfile, " values"] })] })) : null] }) }), SP_JSX.jsx(DFL.PanelSection, { title: "Controls", children: isLoading ? (SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsxs("div", { style: { display: "flex", alignItems: "center", justifyContent: "center" }, children: [SP_JSX.jsx(DFL.Spinner, {}), SP_JSX.jsxs("span", { style: { marginLeft: "10px" }, children: [statusText, "..."] })] }) })) : (SP_JSX.jsxs(SP_JSX.Fragment, { children: [SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", disabled: !gpuStatus.connected || gpuStatus.active || isLoading, onClick: () => handleAction("enable_egpu", "Enabling"), children: gpuStatus.active ? "XG Mobile is Active" : "Enable XG Mobile" }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", disabled: !gpuStatus.active || isLoading, onClick: () => handleAction("eject_egpu", "Ejecting"), children: gpuStatus.active ? "Eject XG Mobile" : "XG Mobile is not Active" }) }), showSleepWarning && (SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsxs("div", { style: {
                                    backgroundColor: 'rgba(255, 0, 0, 0.15)',
                                    border: '1px solid #ff4444',
                                    padding: '12px',
                                    borderRadius: '6px',
                                    color: '#ffdddd',
                                    fontSize: '13px',
                                    lineHeight: '1.4'
                                }, children: [SP_JSX.jsx("b", { children: "\u26A0\uFE0F CRITICAL SLEEP WARNING" }), SP_JSX.jsx("br", {}), "Due to NVIDIA firmware limitations on this OS, putting the device to sleep right now will cause a fatal hardware crash requiring a hard reboot. ", SP_JSX.jsx("br", {}), SP_JSX.jsx("br", {}), SP_JSX.jsx("b", { children: "You must Eject the eGPU before sleeping." })] }) })), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ToggleField, { label: "NVIDIA Mode", description: "Turn off for AMD XG Mobile units", 
                                // Disable if the hardware is active
                                disabled: gpuStatus.active || isLoading, checked: selectedVendor === "nvidia", onChange: toggleVendor }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ToggleField, { label: "Play on Internal Screen", description: "Keep Game Mode on the Ally's built-in display. Games render on the XG Mobile and the picture is sent back to the internal screen. Turn this off to use a monitor plugged into the XG Mobile.", disabled: gpuStatus.active || isLoading, checked: displayTarget === "internal", onChange: toggleDisplayTarget }) }), displayTarget === "internal" && (SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ToggleField, { label: "60 Hz panel lock", description: "Locks the Ally panel at 60 Hz with the game at 60 real frames. Leave this off to keep the panel at 120 Hz so frame generation can fill 120. Eject and Enable to apply.", disabled: gpuStatus.active || isLoading, checked: panel60, onChange: togglePanel60 }) })), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ToggleField, { label: "DesktopMode on Enable", description: "Turn on to reboot straight into Desktop Mode when running the Enable. Recommended for 4K and Ultrawide resolutions with NVIDIA GPUs.", 
                                // Disable if the hardware is active
                                disabled: gpuStatus.active || isLoading, checked: selectedDesktopMode === "1", onChange: toggleDesktopMode }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ToggleField, { label: "Set Desktop to Default", description: "Turn on to set Desktop Mode to Default when running the XG Mobile. You MUST run the Eject Desktop Link to revert to GameMode.", 
                                // Disable if the hardware is active
                                disabled: gpuStatus.active || isLoading || selectedDesktopMode === "0", checked: selectedDesktopDefault === "1", onChange: toggleDesktopDefault }) })] })) }), SP_JSX.jsxs(DFL.PanelSection, { title: "Advanced", children: [SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", description: "Create Desktop shortcuts for enabling and ejecting the XG Mobile. Run this after selecting Vender type and running Install NVIDIA Drivers if needed.", disabled: isLoading, onClick: () => handleAction("create_desktop_shortcuts", "Creating Shortcuts"), children: "Create Desktop Shortcuts" }) }), osType === "steamos" && (SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", disabled: gpuStatus.active || isLoading, onClick: handleInstall, children: "Install NVIDIA Drivers" }) })), osType === "steamos" && (SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", disabled: gpuStatus.active || isLoading, onClick: onResetClick, children: SP_JSX.jsx("span", { style: { color: "#ff5555" }, children: "Reset Driver Environment" }) }) })), deviceType === "laptop" && ( //{hasSupergfxctl && (
                    SP_JSX.jsxs(DFL.PanelSection, { title: "Flow Laptop Controls", children: [SP_JSX.jsxs(DFL.PanelSectionRow, { children: [SP_JSX.jsx(DFL.ButtonItem, { layout: "below", onClick: () => enableSuperGfxd(), children: "Restart Supergfxd" }), SP_JSX.jsx(DFL.ButtonItem, { layout: "below", disabled: isLoading, onClick: () => handleAction("hybrid_supergfxctl", "Switching to Hybrid"), children: "Send Supergfxctl Hybrid command - beta" })] }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", disabled: isLoading, onClick: () => handleAction("integrated_supergfxctl", "Switching to Integrated"), children: "Send Supergfxctl Integrated command - beta" }) })] })), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx(DFL.ButtonItem, { layout: "below", onClick: () => DFL.showModal(SP_JSX.jsx(LogViewerModal, {})), children: "View Activity Logs" }) })] }), SP_JSX.jsxs(DFL.PanelSection, { title: "About", children: [SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsxs("div", { style: { display: "flex", justifyContent: "space-between", opacity: 0.6, fontSize: "0.8em" }, children: [SP_JSX.jsx("span", { children: "Version" }), SP_JSX.jsx("span", { children: pluginVersion })] }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx("div", { style: { opacity: 0.6, fontSize: "0.8em" }, children: "Original plugin by Kenny Hood (Kentronix). This build adds internal screen, efficient boost, and frame pacing." }) }), SP_JSX.jsx(DFL.PanelSectionRow, { children: SP_JSX.jsx("div", { style: { opacity: 0.6, fontSize: "0.8em" }, children: "Changes by tanayabhay-creator. GPL-3." }) })] })] }));
};

var index = definePlugin(() => {
    const pluginState = new PluginState();
    PluginController.setup(pluginState);
    const checkDesktopTransition = async () => {
        try {
            // @decky/api returns the result directly, and handles backend errors natively
            const needsTransition = (await call("check_and_clear_desktop_flag"));
            if (needsTransition) {
                console.log("XG Mobile: Transitioning to Desktop Mode via OS backend...");
                // Give SDDM 4 seconds to stabilize, then let Python execute the native swap
                setTimeout(async () => {
                    await call("trigger_desktop_mode");
                }, 4000);
            }
        }
        catch (e) {
            console.error("XG Mobile Transition check failed:", e);
        }
    };
    const loginUnregisterer = PluginController.initOnLogin(async () => {
        // Initialization logic
    });
    checkDesktopTransition();
    return {
        name: "ASUS XGMobile Manager",
        title: SP_JSX.jsx("div", { className: DFL.staticClasses.Title, children: "ASUS XGMobile Manager" }),
        content: (SP_JSX.jsx(PluginContextProvider, { PluginStateClass: pluginState, children: SP_JSX.jsx(QuickAccessContent, {}) })),
        icon: SP_JSX.jsx(BsGpuCard, {}), //<SiAsus />
        onDismount: () => {
            loginUnregisterer.unregister();
            PluginController.dismount();
        },
    };
});

export { index as default };
//# sourceMappingURL=index.js.map
