﻿using ECSEngine.Assets;
using ImGuiNET;

namespace ECSEngine.Managers.ImGuiWindows.Addons
{
    class AddonBrowserWindow : ImGuiWindow
    {
        public override bool Render { get; set; }
        public override string Title { get; } = "Addon Browser";
        public override string IconGlyph { get; } = FontAwesome5.PuzzlePiece;

        private string[] addonCategories = new[]
        {
            "Gamemodes",
            "Extensions",
            "Models",
            "Textures",
            "Maps",
            "Tools",
            "NPCs",
            "Asset Handlers"
        };

        private int currentCategoryIndex;

        public override void Draw()
        {
            ImGui.PushItemWidth(-1);
            ImGui.ListBox("Categories", ref currentCategoryIndex, addonCategories, addonCategories.Length, addonCategories.Length);
            ImGui.PopItemWidth();
        }
    }
}
