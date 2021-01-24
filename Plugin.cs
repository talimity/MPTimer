using System;
using System.Linq;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;
using MPTimer.Attributes;

namespace MPTimer {
    // ReSharper disable once UnusedMember.Global -- plugin entrypoint
    public class Plugin : IDalamudPlugin {
        public string Name => "MPTimer";

        private const float ActorTickInterval = 3;
        private const double PollingInterval = 1d / 30;
        // StatusEffect IDs as of Patch 5.4
        private const short LucidDreaming = 1204;
        private const short CircleOfPower = 738;

        private DalamudPluginInterface pluginInterface;
        private PluginCommandManager<Plugin> commandManager;
        private Configuration config;
        private PluginUi ui;

        private double lastUpdate;
        private double lastTickTime = 1;
        private int lastMpValue = -1;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration) this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(this.pluginInterface);
            this.ui = new PluginUi(this.config);

            this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);

            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += ShowConfigWindow;
            this.pluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
            this.pluginInterface.ClientState.TerritoryChanged += TerritoryChanged;
        }

        private bool PluginEnabled() {
            const uint blackMageJobId = 25;
            var clientState = this.pluginInterface.ClientState;

            if (clientState.LocalPlayer?.ClassJob.Id != blackMageJobId) return false;
            var inCombat = clientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat];
            if (this.config.HideOutOfCombat && !inCombat) {
                var inDuty = clientState.Condition[Dalamud.Game.ClientState.ConditionFlag.BoundByDuty];
                var battleTarget = clientState.Targets.CurrentTarget?.ObjectKind == ObjectKind.BattleNpc;
                var showingBecauseInDuty = this.config.AlwaysShowInDuties && inDuty;
                var showingBecauseHasTarget = this.config.AlwaysShowWithHostileTarget && battleTarget;
                if (!(showingBecauseInDuty || showingBecauseHasTarget)) {
                    return false;
                }
            }
            return this.config.PluginEnabled;
        }

        private void FrameworkOnOnUpdateEvent(Framework framework) {
            if (!PluginEnabled()) {
                this.ui.BarVisible = false;
                return;
            }
            this.ui.BarVisible = true;

            var now = ImGui.GetTime();
            if (now - lastUpdate < PollingInterval) return;
            lastUpdate = now;

            var state = this.pluginInterface.ClientState;
            var mp = state.LocalPlayer.CurrentMp;
            var gauge = state.JobGauges.Get<BLMGauge>();
            // If Lucid is up (meme optimization tech), just ignore MP gains and rely on the 3 second timer
            var lucidActive = state.LocalPlayer.StatusEffects.Any(e => e.EffectId == LucidDreaming);
            if (!lucidActive && lastMpValue < mp) {
                // Ignore MP gains in Astral Fire, since they're probably Convert/Ether
                if (!gauge.InAstralFire()) lastTickTime = now;
            } else if (lastTickTime + ActorTickInterval <= now) {
                lastTickTime += ActorTickInterval;
            }

            if (this.config.ShowFireThreshold && gauge.InUmbralIce()) {
                var leyLinesActive = state.LocalPlayer.StatusEffects.Any(e => e.EffectId == CircleOfPower);
                // Server grace period, after which casts are committed, Umbral Ice is removed, and slidecasting is possible
                const float gracePeriod = 0.5f;
                // TODO: Make this configurable or figure out the wizardry involved querying the real value
                // 1.5 is a conservative value that should cover most (all?) achievable spellspeed amounts
                const float fastFireCastTime = 1.5f;
                this.ui.FireThreshold = 3 - (fastFireCastTime * (leyLinesActive ? 0.85 : 1)) - gracePeriod;
            } else {
                this.ui.FireThreshold = -1;
            }

            this.ui.LastTick = lastTickTime;
            lastMpValue = mp;
        }

        private void TerritoryChanged(object sender, ushort e) {
            lastMpValue = -1;
        }

        [Command("/mptimer")]
        [HelpMessage("Displays MPTimer's configuration window.")]
        private void ShowConfigWindow(object sender, EventArgs e) {
            this.ui.ConfigVisible = true;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing) {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.SavePluginConfig(this.config);

            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;
            this.pluginInterface.UiBuilder.OnOpenConfigUi -= ShowConfigWindow;
            this.pluginInterface.Framework.OnUpdateEvent -= FrameworkOnOnUpdateEvent;
            this.pluginInterface.ClientState.TerritoryChanged -= TerritoryChanged;

            this.pluginInterface.Dispose();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
