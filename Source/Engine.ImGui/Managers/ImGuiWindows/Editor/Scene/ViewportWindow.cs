﻿using Engine.Assets;
using Engine.Components;
using Engine.Renderer.GL.Entities;
using Engine.Renderer.GL.Managers;
using ImGuiNET;
using System;

namespace Engine.Gui.Managers.ImGuiWindows.Editor
{
    class ViewportWindow : ImGuiWindow
    {
        public override bool Render { get; set; }
        public override string IconGlyph { get; } = FontAwesome5.Play;
        public override string Title { get; } = "Viewport";

        // TODO: Communicate with scene hierarchy, get selected scene camera

        public override void Draw()
        {
            DrawCamera(SceneManager.Instance.MainCamera);
        }

        private void DrawCamera(CameraEntity cameraEntity)
        {
            var windowWidth = ImGui.GetWindowSize().X;
            var windowHeight = ImGui.GetWindowSize().Y;
            var camera = cameraEntity.GetComponent<CameraComponent>();

            var ratio = camera.Resolution.y / camera.Resolution.x;
            var image = camera.Framebuffer.ColorTexture;
            var cameraScale = 1.0f;

            ImGui.Image((IntPtr)image, new System.Numerics.Vector2(windowWidth, windowWidth * ratio) * cameraScale, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));

            // TODO: Fix memory leak
            // camera.Resolution = new Utils.MathUtils.Vector2f(windowWidth, windowHeight);
        }
    }
}
