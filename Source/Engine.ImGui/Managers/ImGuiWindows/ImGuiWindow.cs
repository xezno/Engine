﻿using Engine.ECS.Notify;
using ImGuiNET;
using System.Numerics;

namespace Engine.Gui.Managers.ImGuiWindows
{
    public abstract class ImGuiWindow
    {
        public abstract bool Render { get; set; }
        public abstract string Title { get; }
        public abstract string IconGlyph { get; }
        public virtual ImGuiWindowFlags Flags { get; }
        public abstract void Draw();

        protected void DrawShadowLabel(string str, Vector2 position)
        {
            var shadowOffset = new Vector2(1, 1);

            ImGui.GetBackgroundDrawList().AddText(
                position + shadowOffset, 0x44000000, str); // Shadow
            ImGui.GetBackgroundDrawList().AddText(
                position, 0xFFFFFFFF, str);
        }

        public virtual void OnNotify(NotifyType eventType, INotifyArgs notifyArgs) { }
    }
}