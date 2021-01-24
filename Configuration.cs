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
        
        [JsonIgnore] private DalamudPluginInterface pluginInterface;
        
        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pluginInterface = pluginInterface;
        }

        public void Save() {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
