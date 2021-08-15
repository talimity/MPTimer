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

        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface dpi) {
            this.pluginInterface = dpi;
        }

        public void Save() {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
