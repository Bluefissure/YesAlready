﻿using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace YesAlready
{
    internal class PluginUI : IDisposable
    {
        private readonly YesAlreadyPlugin plugin;

        public PluginUI(YesAlreadyPlugin plugin)
        {
            this.plugin = plugin;

            plugin.Interface.UiBuilder.OnOpenConfigUi += UiBuilder_OnOpenConfigUi;
            plugin.Interface.UiBuilder.OnBuildUi += UiBuilder_OnBuildUi;
        }

        public void Dispose()
        {
            plugin.Interface.UiBuilder.OnOpenConfigUi -= UiBuilder_OnOpenConfigUi;
            plugin.Interface.UiBuilder.OnBuildUi -= UiBuilder_OnBuildUi;
        }

#if DEBUG
        private bool IsImguiSetupOpen = true;
#else
        private bool IsImguiSetupOpen = false;
#endif

        public void Open() => IsImguiSetupOpen = true;

        public void UiBuilder_OnOpenConfigUi(object sender, EventArgs args) => IsImguiSetupOpen = true;

        public void UiBuilder_OnBuildUi()
        {
            if (!IsImguiSetupOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(525, 600), ImGuiCond.FirstUseEver);

            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);

            if (!ImGui.Begin(plugin.Name, ref IsImguiSetupOpen))
                return;

            if (ImGui.Checkbox($"Enabled", ref plugin.Configuration.Enabled))
                plugin.SaveConfiguration();

            UiBuilder_TextEntryButtons();
            UiBuilder_TextEntries();
            UiBuilder_TextEntryOptionsPopup();
            UiBuilder_TextEntryQuickDelete();

            UiBuilder_ItemsWithoutText();

            ImGui.End();

            ImGui.PopStyleColor();
        }

        private void UiBuilder_TextEntryButtons()
        {
            if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add new entry"))
            {
                plugin.Configuration.TextEntries.Insert(0, new() { Text = "" });
                plugin.SaveConfiguration();
            }

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.SearchPlus, "Add last seen as new entry"))
            {
                plugin.Configuration.TextEntries.Insert(0, new() { Text = plugin.LastSeenDialogText });
                plugin.SaveConfiguration();
            }

            var sb = new StringBuilder();
            sb.AppendLine("Enter into the input all or part of the text inside a dialog.");
            sb.AppendLine("For example: \"Teleport to \" for the teleport dialog.");
            sb.AppendLine();
            sb.AppendLine("Alternatively, wrap your text in forward slashes to use as a regex.");
            sb.AppendLine("As such: \"/Teleport to .*? for \\d+ gil\\?/\"");
            sb.AppendLine();
            sb.AppendLine("If it matches, the yes button (and checkbox if present) will be clicked.");
            sb.AppendLine();
            sb.AppendLine("Right click the enabled button to view options.");
            sb.AppendLine("Ctrl-Shift right click the enabled button to delete that entry.");
            sb.AppendLine();
            sb.AppendLine("Currently supported text addons:");
            sb.AppendLine("  - SelectYesNo");
            sb.AppendLine();
            sb.AppendLine("Non-text addons are each listed separately in the lower config section.");

            ImGui.SameLine();
            ImGuiEx.IconButton(FontAwesomeIcon.QuestionCircle, sb.ToString());
        }

        private void UiBuilder_TextEntries()
        {
            if (plugin.Configuration.TextEntries.Count == 0)
            {
                plugin.Configuration.TextEntries.Add(new());
                plugin.SaveConfiguration();
            }

            if (ImGui.CollapsingHeader("Unassigned"))
            {
                var entries = plugin.Configuration.TextEntries.Where(entry => entry.Folder == "");
                foreach (var entry in entries)
                    UiBuilder_TextEntry(entry);
            }

            var groups = plugin.Configuration.TextEntries.Where(entry => entry.Folder != "").GroupBy(entry => entry.Folder);
            foreach (var group in groups)
            {
                if (ImGui.CollapsingHeader(group.Key))
                {
                    foreach (var entry in group)
                        UiBuilder_TextEntry(entry);
                }
            }
        }

        private void UiBuilder_TextEntryOptionsPopup()
        {
            var entry = TextEntryOptionsTarget;

            if (ImGui.BeginPopupContextItem("EntryOptions"))
            {
                if (ImGui.InputText("Folder", ref entry.Folder, 100))
                    plugin.SaveConfiguration();

                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
                {
                    plugin.Configuration.TextEntries.Remove(entry);
                    plugin.SaveConfiguration();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        private void UiBuilder_TextEntryQuickDelete()
        {
            var entry = TextEntryQuickDeleteTarget;

            if (entry != null)
            {
                plugin.Configuration.TextEntries.Remove(entry);
                plugin.SaveConfiguration();
                TextEntryQuickDeleteTarget = null;
            }
        }

        private ConfigTextEntry TextEntryOptionsTarget = null;
        private ConfigTextEntry TextEntryQuickDeleteTarget = null;

        private void UiBuilder_TextEntry(ConfigTextEntry entry)
        {
            if (ImGui.Checkbox($"###enabled-{entry.GetHashCode()}", ref entry.Enabled))
                plugin.SaveConfiguration();
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                var io = ImGui.GetIO();
                if (io.KeyCtrl && io.KeyShift)
                {
                    TextEntryQuickDeleteTarget = entry;
                }
                else
                {
                    TextEntryOptionsTarget = entry;
                    ImGui.OpenPopup("EntryOptions");
                }
            }

            ImGuiEx.TextTooltip("Enabled");

            if (entry.IsRegex && entry.Regex == null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
                ImGui.PushFont(UiBuilder.IconFont);

                ImGui.SameLine();
                ImGui.Text(FontAwesomeIcon.Exclamation.ToIconString());

                ImGui.PopFont();
                ImGui.PopStyleColor();

                ImGuiEx.TextTooltip("Invalid Regex");
            }

            ImGui.PushItemWidth(-1);

            ImGui.SameLine();
            if (ImGui.InputText($"###text-{entry.GetHashCode()}", ref entry.Text, 10_000) && (!entry.IsRegex || entry.Regex != null))
                plugin.SaveConfiguration();

            ImGui.PopItemWidth();
        }

        private void UiBuilder_ItemsWithoutText()
        {
            if (ImGui.CollapsingHeader("Non-text Matching"))
            {
                if (ImGui.Checkbox("Desynthesis", ref plugin.Configuration.DesynthDialogEnabled))
                    plugin.SaveConfiguration();
                ImGuiEx.TextTooltip("Don't blame me when you destroy something important");

                if (ImGui.Checkbox("Materialize", ref plugin.Configuration.MaterializeDialogEnabled))
                    plugin.SaveConfiguration();
                ImGuiEx.TextTooltip("The dialog that extracts materia from items");

                if (ImGui.Checkbox("Item Inspection Result", ref plugin.Configuration.ItemInspectionResultEnabled))
                    plugin.SaveConfiguration();
                ImGuiEx.TextTooltip("Eureka/Bozja lockboxes, forgotten fragments, and more.");

                if (ImGui.Checkbox("Assign on Retainer Venture Request", ref plugin.Configuration.RetainerTaskAskEnabled))
                    plugin.SaveConfiguration();
                ImGuiEx.TextTooltip("The final dialog before sending out a retainer.");

                if (ImGui.Checkbox("Reassign on Retainer Venture Result", ref plugin.Configuration.RetainerTaskResultEnabled))
                    plugin.SaveConfiguration();
                ImGuiEx.TextTooltip("Where you receive the item and can resend on the same task.");
            }
        }
    }

    internal static class ImGuiEx
    {
        public static bool IconButton(FontAwesomeIcon icon) => IconButton(icon);

        public static bool IconButton(FontAwesomeIcon icon, string tooltip)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}");
            ImGui.PopFont();

            if (tooltip != null)
                TextTooltip(tooltip);

            return result;
        }

        public static void TextTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(text);
                ImGui.EndTooltip();
            }
        }

        #region rotation

        private static int rotation_start_index;

        public static Vector2 Min(Vector2 lhs, Vector2 rhs) => new(lhs.X < rhs.X ? lhs.X : rhs.X, lhs.Y < rhs.Y ? lhs.Y : rhs.Y);

        public static Vector2 Max(Vector2 lhs, Vector2 rhs) => new(lhs.X >= rhs.X ? lhs.X : rhs.X, lhs.Y >= rhs.Y ? lhs.Y : rhs.Y);

        private static Vector2 Rotate(Vector2 v, float cos_a, float sin_a) => new(v.X * cos_a - v.Y * sin_a, v.X * sin_a + v.Y * cos_a);

        public static void RotateStart()
        {
            rotation_start_index = ImGui.GetWindowDrawList().VtxBuffer.Size;
        }

        public static void RotateEnd(double rad) => RotateEnd(rad, RotationCenter());

        public static void RotateEnd(double rad, Vector2 center)
        {
            var sin = (float)Math.Sin(rad);
            var cos = (float)Math.Cos(rad);
            center = Rotate(center, sin, cos) - center;

            var buf = ImGui.GetWindowDrawList().VtxBuffer;
            for (int i = rotation_start_index; i < buf.Size; i++)
                buf[i].pos = Rotate(buf[i].pos, sin, cos) - center;
        }

        private static Vector2 RotationCenter()
        {
            var l = new Vector2(float.MaxValue, float.MaxValue);
            var u = new Vector2(float.MinValue, float.MinValue);

            var buf = ImGui.GetWindowDrawList().VtxBuffer;
            for (int i = rotation_start_index; i < buf.Size; i++)
            {
                l = Min(l, buf[i].pos);
                u = Max(u, buf[i].pos);
            }

            return new Vector2((l.X + u.X) / 2, (l.Y + u.Y) / 2);
        }

        #endregion
    }
}
