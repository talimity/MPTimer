using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Interface.Components;
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
        private readonly Vector2 barFillSizeOffset = new Vector2(-1, 0);
        private readonly Vector2 barWindowPadding = new Vector2(8, 14);
        private readonly Vector2 configInitialSize = new Vector2(300, 350);
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
            ImGui.SetNextWindowSize(this.config.BarSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(this.config.BarPosition, ImGuiCond.FirstUseEver);
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
            var barBackgroundColor = ImGui.GetColorU32(this.config.BarBackgroundColor);
            var barFillColor = ImGui.GetColorU32(this.config.BarFillColor);
            var barBorderColor = ImGui.GetColorU32(this.config.BarBorderColor);
            drawList.AddRectFilled(topLeft + barFillPosOffset, bottomRight + barFillSizeOffset, barBackgroundColor);
            drawList.AddRectFilled(topLeft + barFillPosOffset, filledSegmentEnd, barFillColor);
            drawList.AddRect(topLeft, bottomRight, barBorderColor, cornerSize, ImDrawFlags.RoundCornersAll,
                borderThickness);

            // Draw Fire III threshold mark
            if (FireThreshold > 0) {
                var thresholdFillColor = ImGui.GetColorU32(this.config.FireThresholdColor);
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

            if (ImGui.BeginTabBar("ConfigTabBar", ImGuiTabBarFlags.None)) {
                DrawAppearanceTab();
                DrawBehaviorTab();
                ImGui.EndTabBar();
            }
            ImGui.Separator();

            ImGui.Spacing();

            ImGui.TextWrapped("※Upon entering a new zone or duty, you should cast Blizzard II to re-sync the timer " +
                              "with your character's MP tick.\nOtherwise, it may be inaccurate until your first " +
                              "Umbral Ice phase.");

            ImGui.End();
        }

        private void DrawAppearanceTab() {
            if (ImGui.BeginTabItem("Appearance")) {
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

                ImGui.Text("Fire III Threshold Color");
                ImGui.SameLine();
                var newFireThresholdColor = ImGuiComponents.ColorPickerWithPalette(1, "Fire III Threshold Color",
                    this.config.FireThresholdColor);
                ImGui.SameLine();
                if (ImGui.Button("Reset##ResetFireThresholdColor")) {
                    this.config.ResetPropertyToDefault("FireThresholdColor");
                    newFireThresholdColor = this.config.FireThresholdColor;
                }

                ImGui.Text("Bar Background Color");
                ImGui.SameLine();
                var newBarBackgroundColor = ImGuiComponents.ColorPickerWithPalette(2, "Bar Background Color",
                    this.config.BarBackgroundColor);
                ImGui.SameLine();
                if (ImGui.Button("Reset##ResetBarBackgroundColor")) {
                    this.config.ResetPropertyToDefault("BarBackgroundColor");
                    newBarBackgroundColor = this.config.BarBackgroundColor;
                }

                ImGui.Text("Bar Fill Color");
                ImGui.SameLine();
                var newBarFillColor = ImGuiComponents.ColorPickerWithPalette(3, "Bar Fill Color",
                    this.config.BarFillColor);
                ImGui.SameLine();
                if (ImGui.Button("Reset##ResetBarFillColor")) {
                    this.config.ResetPropertyToDefault("BarFillColor");
                    newBarFillColor = this.config.BarFillColor;
                }

                ImGui.Text("Bar Border Color");
                ImGui.SameLine();
                var newBarBorderColor = ImGuiComponents.ColorPickerWithPalette(4, "Bar Border Color",
                    this.config.BarBorderColor);
                ImGui.SameLine();
                if (ImGui.Button("Reset##ResetBarBorderColor")) {
                    this.config.ResetPropertyToDefault("BarBorderColor");
                    newBarBorderColor = this.config.BarBorderColor;
                }

                if (!newFireThresholdColor.Equals(this.config.FireThresholdColor) ||
                    !newBarBackgroundColor.Equals(this.config.BarBackgroundColor) ||
                    !newBarFillColor.Equals(this.config.BarFillColor) ||
                    !newBarBorderColor.Equals(this.config.BarBorderColor)) {
                    this.config.FireThresholdColor = newFireThresholdColor;
                    this.config.BarBackgroundColor = newBarBackgroundColor;
                    this.config.BarFillColor = newBarFillColor;
                    this.config.BarBorderColor = newBarBorderColor;
                    this.config.Save();
                }

                ImGui.EndTabItem();
            }
        }

        private void DrawBehaviorTab() {
            if (ImGui.BeginTabItem("Behavior")) {
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

                ImGui.EndTabItem();
            }
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
