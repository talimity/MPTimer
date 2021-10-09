using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using MPTimer.Attributes;


namespace MPTimer {
    // ReSharper disable once UnusedType.Global -- plugin entrypoint
    public class Plugin : IDalamudPlugin {
        public string Name => "MPTimer";

        private const float ActorTickInterval = 3;
        private const double PollingInterval = 1d / 30;
        // StatusEffect and action IDs as of Patch 5.58
        private const short LucidDreaming = 1204;
        private const short CircleOfPower = 738;
        private const uint Fire3 = 152;

        [PluginService] public DalamudPluginInterface Interface { get; private set; }
        private PluginCommandManager<Plugin> commandManager;
        private Configuration config;
        private PluginUi ui;

        [PluginService] public ClientState State { get; private set; }
        [PluginService] public Framework Framework { get; private set; }
        [PluginService] public static Condition Condition { get; private set; }
        [PluginService] public static JobGauges JobGauges { get; private set; }

        private double lastUpdate;
        private double lastTickTime = 1;
        private int lastMpValue = -1;

        public Plugin(CommandManager command) {
            this.config = (Configuration)this.Interface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(this.Interface);
            
            Resolver.Initialize();

            this.commandManager = new PluginCommandManager<Plugin>(this, command);

            this.ui = new PluginUi(this.config);
            this.Interface.UiBuilder.Draw += this.ui.Draw;
            this.Interface.UiBuilder.OpenConfigUi += OpenConfigUi;
            Framework.Update += FrameworkOnOnUpdateEvent;
            State.TerritoryChanged += TerritoryChanged;
        }

        private bool PluginEnabled() {
            const uint blackMageJobId = 25;

            if (State.LocalPlayer?.ClassJob.Id != blackMageJobId) return false;
            var inCombat = Condition[ConditionFlag.InCombat];
            if (this.config.HideOutOfCombat && !inCombat) {
                var inDuty = Condition[ConditionFlag.BoundByDuty];
                var battleTarget = State.LocalPlayer.TargetObject?.ObjectKind == ObjectKind.BattleNpc;
                var showingBecauseInDuty = this.config.AlwaysShowInDuties && inDuty;
                var showingBecauseHasTarget = this.config.AlwaysShowWithHostileTarget && battleTarget;
                if (!(showingBecauseInDuty || showingBecauseHasTarget)) {
                    return false;
                }
            }
            return this.config.PluginEnabled;
        }

        private unsafe void FrameworkOnOnUpdateEvent(Framework framework) {
            if (!PluginEnabled()) {
                this.ui.BarVisible = false;
                return;
            }
            this.ui.BarVisible = true;

            var now = ImGui.GetTime();
            if (now - lastUpdate < PollingInterval) return;
            lastUpdate = now;
            
            PluginLog.Information("getAdjustedCastTime: " + ActionManager.Instance()->GetAdjustedRecastTime(ActionType.Spell, Fire3));
            this.ui.FireCastTime = ActionManager.Instance()->GetAdjustedRecastTime(ActionType.Spell, Fire3);

            var mp = State.LocalPlayer.CurrentMp;
            var gauge = JobGauges.Get<BLMGauge>();
            // If Lucid is up (meme optimization tech), just ignore MP gains and rely on the 3 second timer
            var lucidActive = State.LocalPlayer.StatusList.Any(e => e.StatusId == LucidDreaming);
            if (!lucidActive && lastMpValue < mp) {
                // Ignore MP gains in Astral Fire, since they're probably Convert/Ether
                if (!gauge.InAstralFire) lastTickTime = now;
            } else if (lastTickTime + ActorTickInterval <= now) {
                lastTickTime += ActorTickInterval;
            }

            if (this.config.ShowFireThreshold && gauge.InUmbralIce) {
                var leyLinesActive = State.LocalPlayer.StatusList.Any(e => e.StatusId == CircleOfPower);
                // Server grace period, after which casts are committed, Umbral Ice is removed, and slidecasting is possible
                const float gracePeriod = 0.5f;
                // TODO: Make this configurable or figure out the wizardry involved querying the real value
                // 1.5 is a conservative value that should cover most (all?) achievable spellspeed amounts
                const float fastFireCastTime = 1.5f;
                this.ui.FireThreshold = 3 - (fastFireCastTime * (leyLinesActive ? 0.85 : 1) - gracePeriod);
            } else {
                this.ui.FireThreshold = -1;
            }

            this.ui.LastTick = lastTickTime;
            lastMpValue = (int)mp;
        }

        private void TerritoryChanged(object sender, ushort e) {
            lastMpValue = -1;
        }

        private void OpenConfigUi() {
            ShowConfigWindow();
        }

        [Command("/mptimer")]
        [HelpMessage("Displays MPTimer's configuration window.")]
        public void DefaultCommand(string command, string args) {
            ShowConfigWindow();
        }

        private void ShowConfigWindow() {
            this.ui.ConfigVisible = true;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing) {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.Interface.SavePluginConfig(this.config);

            this.Interface.UiBuilder.Draw -= this.ui.Draw;
            this.Interface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            Framework.Update -= FrameworkOnOnUpdateEvent;
            State.TerritoryChanged -= TerritoryChanged;

            this.Interface.Dispose();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
