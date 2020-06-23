﻿using Engine.Gui.Managers.ImGuiWindows;
using System.Collections.Generic;

namespace Engine.Gui.Managers
{
    public class ImGuiMenu
    {
        public string Title { get; }
        public string IconGlyph { get; }

        public List<ImGuiWindow> windows;

        public ImGuiMenu(string iconGlyph, string title, List<ImGuiWindow> windows)
        {
            Title = title;
            IconGlyph = iconGlyph;
            this.windows = windows;
        }
    }
}