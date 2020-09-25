﻿using Engine.ECS.Components;
using OpenGL;
using Quincy.Entities;
using Quincy.Primitives;

namespace Quincy.Components
{
    public class SkyboxComponent : Component<SkyboxComponent>
    {
        public Cubemap skybox;
        public Cubemap convolutedSkybox;
        public Cubemap prefilteredSkybox;

        private ShaderComponent skyboxShader;
        private Cube skyboxCube;

        public SkyboxComponent(string hdriPath)
        {
            var cubemaps = EquirectangularToCubemap.Convert(hdriPath);
            skybox = cubemaps.Item1;
            convolutedSkybox = cubemaps.Item2;
            prefilteredSkybox = cubemaps.Item3;

            skyboxShader = new ShaderComponent("Content/Shaders/Skybox/skybox.frag", "Content/Shaders/Skybox/skybox.vert");
            skyboxCube = new Cube();
        }

        public void DrawSkybox(CameraEntity camera)
        {
            Gl.Disable(EnableCap.CullFace);
            var modelMatrix = Matrix4x4f.Identity;
            var scale = 10000.0f;
            modelMatrix.Scale(scale, scale, scale);
            skyboxShader.Use();
            skyboxShader.SetMatrix("projMatrix", camera.GetComponent<CameraComponent>().ProjMatrix);
            skyboxShader.SetMatrix("viewMatrix", camera.GetComponent<CameraComponent>().ViewMatrix);
            skyboxShader.SetMatrix("modelMatrix", modelMatrix);
            skyboxShader.SetInt("environmentMap", 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.TextureCubeMap, skybox.Id);

            skyboxCube.Draw();

            Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
            Gl.Enable(EnableCap.CullFace);
        }
    }
}
