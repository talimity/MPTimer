using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;

namespace MPTimer {
    public class PluginUi {
        public bool BarVisible { get; set; }
        private bool configVisible;
        public bool ConfigVisible {
            get => this.configVisible;
            set => this.configVisible = value;
        }
        public double LastTick = 1;
        public double FireThreshold = -1;
        public double FireCastTime = 0;

        private const float ActorTickInterval = 3;
        private const ImGuiWindowFlags LockedBarFlags = ImGuiWindowFlags.NoBackground |
                                                        ImGuiWindowFlags.NoMove |
                                                        ImGuiWindowFlags.NoResize |
                                                        ImGuiWindowFlags.NoNav |
                                                        ImGuiWindowFlags.NoInputs;
        private const string MainWindowName = "MPTimerBar";

        private readonly Configuration config;
        private readonly Vector2 barFillPosOffset = new Vector2(1, 1);
        private readonly Vector2 barFillSizeOffset = new Vector2(-2, -2);
        private readonly Vector2 barWindowPadding = new Vector2(8, 14);
        private readonly Vector2 barInitialSize = new Vector2(190, 40);
        private readonly Vector2 barInitialPos = new Vector2(800, 500);
        private readonly Vector2 configInitialSize = new Vector2(300, 310);
        private readonly uint barBorderColor = ImGui.GetColorU32(new Vector4(0.451f, 0.796f, 0.969f, 1f));
        private readonly uint barBackgroundColor = ImGui.GetColorU32(new Vector4(0.031f, 0.173f, 0.332f, 1f));
        private readonly uint barFillColor = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f));
        private readonly uint thresholdFillColor = ImGui.GetColorU32(new Vector4(1f, 0.650f, 0.133f, 1f));
        private double now;

        public PluginUi(Configuration config) {
            this.config = config;
        }

        public void Draw() {
            this.now = ImGui.GetTime();

            if (BarVisible) DrawBarWindow();
            if (ConfigVisible) DrawConfigWindow();
        }

        private void DrawBarWindow() {
            ImGui.SetNextWindowSize(barInitialSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(barInitialPos, ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, barWindowPadding);
            var windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;
            if (this.config.LockBar) windowFlags |= LockedBarFlags;
            ImGui.Begin(MainWindowName, windowFlags);
            UpdateSavedWindowConfig(ImGui.GetWindowPos(), ImGui.GetWindowSize());

            var progress = (float)((now - LastTick) / ActorTickInterval);
            if (progress > 1) progress = 1;

            // Setup bar rects
            var topLeft = ImGui.GetWindowContentRegionMin();
            var bottomRight = ImGui.GetWindowContentRegionMax();
            var barWidth = bottomRight.X - topLeft.X;
            var filledSegmentEnd = new Vector2(barWidth * progress + barWindowPadding.X, bottomRight.Y - 1);

            // Convert imgui window-space rects to screen-space
            var windowPosition = ImGui.GetWindowPos();
            topLeft += windowPosition;
            bottomRight += windowPosition;
            filledSegmentEnd += windowPosition;

            // Draw main bar
            const float cornerSize = 2f;
            const float borderThickness = 1.35f;
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(topLeft + barFillPosOffset, bottomRight + barFillSizeOffset, barBackgroundColor);
            drawList.AddRectFilled(topLeft + barFillPosOffset, filledSegmentEnd, barFillColor);
            drawList.AddRect(topLeft, bottomRight, barBorderColor, cornerSize, ImDrawFlags.RoundCornersAll,
                borderThickness);

            // Draw Fire III threshold mark
            if (FireThreshold > 0) {
                const float lineThickness = 4;
                var thresholdProgress = (float)FireThreshold / ActorTickInterval;
                var thresholdPosition = barWidth * thresholdProgress + barWindowPadding.X + windowPosition.X;
                var thresholdTop = new Vector2(thresholdPosition, topLeft.Y + barFillPosOffset.Y);
                var thresholdBottom = new Vector2(thresholdPosition + lineThickness, bottomRight.Y - 1);
                drawList.AddRectFilled(thresholdTop, thresholdBottom, thresholdFillColor);
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

        private void DrawConfigWindow() {
            ImGui.SetNextWindowSize(configInitialSize, ImGuiCond.Appearing);
            ImGui.Begin("MPTimer Settings", ref this.configVisible);
            
            ImGui.Text("Fire 3 Cast Time: " + FireCastTime);

            var pluginEnabled = this.config.PluginEnabled;
            if (ImGui.Checkbox("Enable plugin", ref pluginEnabled)) {
                this.config.PluginEnabled = pluginEnabled;
                this.config.Save();
            }

            var lockBar = this.config.LockBar;
            if (ImGui.Checkbox("Lock bar size and position", ref lockBar)) {
                this.config.LockBar = lockBar;
                this.config.Save();
            }

            if (!this.config.LockBar) {
                ImGui.Indent();
                int[] barPosition = { (int)this.config.BarPosition.X, (int)this.config.BarPosition.Y };
                if (ImGui.DragInt2("Position", ref barPosition[0])) {
                    ImGui.SetWindowPos(MainWindowName, new Vector2(barPosition[0], barPosition[1]));
                }

                int[] barSize = { (int)this.config.BarSize.X, (int)this.config.BarSize.Y };
                if (ImGui.DragInt2("Size", ref barSize[0])) {
                    ImGui.SetWindowSize(MainWindowName, new Vector2(barSize[0], barSize[1]));
                }
                ImGui.Unindent();
            }

            var showFireThreshold = this.config.ShowFireThreshold;
            if (ImGui.Checkbox("Show Fire III cast threshold", ref showFireThreshold)) {
                this.config.ShowFireThreshold = showFireThreshold;
                this.config.Save();
            }

            var hideOutOfCombat = this.config.HideOutOfCombat;
            if (ImGui.Checkbox("Hide while not in combat", ref hideOutOfCombat)) {
                this.config.HideOutOfCombat = hideOutOfCombat;
                this.config.Save();
            }

            ImGui.Indent();
            var showInDuties = this.config.AlwaysShowInDuties;
            if (ImGui.Checkbox("Always show while in duties", ref showInDuties)) {
                this.config.AlwaysShowInDuties = showInDuties;
                this.config.Save();
            }

            var showWithHostileTarget = this.config.AlwaysShowWithHostileTarget;
            if (ImGui.Checkbox("Always show with enemy target", ref showWithHostileTarget)) {
                this.config.AlwaysShowWithHostileTarget = showWithHostileTarget;
                this.config.Save();
            }
            ImGui.Unindent();

            ImGui.Spacing();
            ImGui.TextWrapped("※Upon entering a new zone or duty, you should cast Blizzard II to re-sync the timer " +
                              "with your character's MP tick.\nOtherwise, it may be inaccurate until your first " +
                              "Umbral Ice phase.");

            ImGui.End();
        }

        private void UpdateSavedWindowConfig(Vector2 currentPos, Vector2 currentSize) {
            if (this.config.LockBar ||
                currentPos.Equals(this.config.BarPosition) && currentSize.Equals(this.config.BarSize)) {
                return;
            }
            this.config.BarPosition = currentPos;
            this.config.BarSize = currentSize;
            this.config.Save();
        }
    }
}
