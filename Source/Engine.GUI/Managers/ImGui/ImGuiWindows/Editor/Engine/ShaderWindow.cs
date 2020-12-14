﻿using Engine.Assets;
using Engine.Renderer.Components;
using Engine.Renderer.Managers;
using ImGuiNET;

namespace Engine.GUI.Managers.ImGuiWindows.Editor
{
    [ImGuiMenuPath(ImGuiMenus.Menu.Engine)]
    class ShaderWindow : ImGuiWindow
    {
        public override bool Render { get; set; } = true;
        public override string IconGlyph { get; } = FontAwesome5.Adjust;
        public override string Title { get; } = "Shaders";

        public ShaderWindow() { }

        public override void Draw()
        {
            if (ImGui.Button("Reload all shaders"))
            {
                ReloadAllShaders();
            }
        }

        private void ReloadAllShaders()
        {
            foreach (var entity in SceneManager.Instance.Entities)
            {
                if (entity.HasComponent<ShaderComponent>())
                {
                    entity.GetComponent<ShaderComponent>().Load();
                }
            }
        }
    }
}
