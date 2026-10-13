import { SiAsus } from "react-icons/si";
import { BsGpuCard } from "react-icons/bs"; 
import { definePlugin, call } from "@decky/api"; 
import { staticClasses } from "@decky/ui";
import React from "react";

import { PluginController } from "./lib/controllers/PluginController";
import { PluginContextProvider } from "./state/PluginContext";
import { PluginState } from "./state/PluginState";
import { QuickAccessContent } from "./components/QuickAccessContent";

export default definePlugin(() => {
  const pluginState = new PluginState();
  PluginController.setup(pluginState);

  const checkDesktopTransition = async () => {
    try {
      // @decky/api returns the result directly, and handles backend errors natively
      const needsTransition = (await call("check_and_clear_desktop_flag")) as boolean;
      
      if (needsTransition) {
        console.log("XG Mobile: Transitioning to Desktop Mode via OS backend...");
        
        // Give SDDM 4 seconds to stabilize, then let Python execute the native swap
        setTimeout(async () => {
          await call("trigger_desktop_mode");
        }, 4000);
      }
    } catch (e) {
      console.error("XG Mobile Transition check failed:", e);
    }
  };

  const loginUnregisterer = PluginController.initOnLogin(async () => {
    // Initialization logic
  });

  checkDesktopTransition();

  return {
    name: "ASUS XGMobile Manager",
    title: <div className={staticClasses.Title}>ASUS XGMobile Manager</div>,
    content: (
      <PluginContextProvider PluginStateClass={pluginState}>
        <QuickAccessContent />
      </PluginContextProvider>
    ),
    icon: <BsGpuCard />, //<SiAsus />
    onDismount: () => {
      loginUnregisterer.unregister();
      PluginController.dismount();
    },
  };
});
