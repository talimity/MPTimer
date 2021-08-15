using System;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace MPTimer {
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; }

        public bool PluginEnabled { get; set; } = true;
        public bool LockBar { get; set; }
        public bool ShowFireThreshold { get; set; }
        public bool HideOutOfCombat { get; set; }
        public bool AlwaysShowInDuties { get; set; }
        public bool AlwaysShowWithHostileTarget { get; set; }
        public Vector2 BarSize { get; set; } = new Vector2(190, 40);
        public Vector2 BarPosition { get; set; } = new Vector2(800, 500);
        public Vector4 FireThresholdColor { get; set; } = new Vector4(1f, 0.650f, 0.133f, 1f);
        public Vector4 BarBorderColor { get; set; } = new Vector4(0.451f, 0.796f, 0.969f, 1f);
        public Vector4 BarBackgroundColor { get; set; } = new Vector4(0.031f, 0.173f, 0.332f, 1f);
        public Vector4 BarFillColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface dpi) {
            this.pluginInterface = dpi;
        }

        public void Save() {
            this.pluginInterface.SavePluginConfig(this);
        }

        public void ResetPropertyToDefault(string colorProp) {
            var configType = this.GetType();
            var instance = Activator.CreateInstance(configType);
            var defaultValue = configType.GetProperty(colorProp)?.GetValue(instance);
            configType.GetProperty(colorProp)?.SetValue(this, defaultValue);
            this.Save();
        }
    }
}
