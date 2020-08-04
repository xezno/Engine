﻿using Engine.Components;
using Engine.ECS.Entities;
using Engine.ECS.Managers;
using Engine.Renderer.GL.Components;
using Engine.Renderer.GL.Entities;
using Engine.Renderer.GL.Render;
using Engine.Utils;
using Engine.Utils.MathUtils;
using OpenGL;
using System;
using System.Threading;

namespace Engine.Renderer.GL.Managers
{
    public class RenderManager : Manager<RenderManager>
    {
        private DateTime lastRender;
        private int currentFrametimeIndex;
        private int currentFramerateIndex;

        private Framebuffer mainFramebuffer;

        private const int FramesToCount = 480;
        private readonly Renderer renderer;

        private ShaderComponent shadowShaders;

        public float LastFrameTime { get; private set; }
        public int CalculatedFramerate => (int)(1000f / Math.Max(LastFrameTime, 0.001f));
        public float[] FrametimeHistory { get; } = new float[FramesToCount];
        public float[] FramerateHistory { get; } = new float[FramesToCount];

        public bool RenderShadowMap { get; set; }
        public bool Paused { get; set; }

        public RenderManager()
        {
            renderer = new Renderer();
            renderer.Init();
            mainFramebuffer = new Framebuffer(GameSettings.GameResolutionX, GameSettings.GameResolutionY);
            shadowShaders = new ShaderComponent(new Shader("Content/Shaders/Depth/depth.frag", Shader.Type.FragmentShader),
                new Shader("Content/Shaders/Depth/depth.vert", Shader.Type.VertexShader));
        }

        private void RenderScene(Matrix4x4f projMatrix, Matrix4x4f viewMatrix, Vector3d cameraPosition)
        {
            foreach (var entity in SceneManager.Instance.Entities)
            {
                if (entity.Enabled)
                {
                    if (entity.HasComponent<MeshComponent>())
                    {
                        RenderMesh(entity, projMatrix, viewMatrix, cameraPosition);
                    }
                }

                // "Legacy": render any entities with custom render code
                // entity.Render();
            }
        }

        private void RenderLights(LightComponent lightComponent)
        {
            foreach (var entity in SceneManager.Instance.Entities)
            {
                if (entity.Enabled)
                {
                    if (entity.HasComponent<MeshComponent>())
                    {
                        RenderMeshShadow(entity, lightComponent.lightMatrix);
                    }
                }
            }
        }

        public void RenderCefComponent(CefComponent cefComponent)
        {
            // render cef offscreen & then blit to screen
            // we need to set up texture on the main therad
            // since that wont happen unless we call it here, we need to
            // declare a bool that allows us to detect when we need to
            // setup the texture.

            if (!cefComponent.ReadyToDraw) return;

            Gl.Disable(EnableCap.DepthTest);

            // draw to screen
            cefComponent.Render();

            cefComponent.SetTextureData();

            Gl.Enable(EnableCap.DepthTest);
        }

        private void RenderCef()
        {
            foreach (var entity in SceneManager.Instance.Entities)
            {
                if (entity.Enabled && entity.HasComponent<CefComponent>())
                {
                    RenderCefComponent(entity.GetComponent<CefComponent>());
                }
            }
        }

        private void BindMatrices(ShaderComponent shaderComponent, Matrix4x4f projMatrix, Matrix4x4f viewMatrix)
        {
            shaderComponent.SetVariable("projMatrix", projMatrix);
            shaderComponent.SetVariable("viewMatrix", viewMatrix);
        }

        private void RenderMeshShadow(IEntity entity, Matrix4x4f lightMatrix)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();

            shadowShaders.UseShader();

            Gl.BindVertexArray(meshComponent.RenderMesh.vao);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, meshComponent.RenderMesh.vbo);

            shadowShaders.SetVariable("lightMatrix", lightMatrix);
            shadowShaders.SetVariable("modelMatrix", transformComponent.Matrix);

            Gl.DrawArrays(PrimitiveType.Triangles, 0, meshComponent.RenderMesh.ElementCount * sizeof(float));
            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void RenderMesh(IEntity entity, Matrix4x4f projMatrix, Matrix4x4f viewMatrix, Vector3d cameraPosition)
        {
            var shaderComponent = entity.GetComponent<ShaderComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();

            shaderComponent.UseShader();

            Gl.BindVertexArray(meshComponent.RenderMesh.vao);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, meshComponent.RenderMesh.vbo);

            BindMatrices(shaderComponent, projMatrix, viewMatrix);

            shaderComponent.SetVariable("cameraPos", cameraPosition.ToVector3());
            shaderComponent.SetVariable("modelMatrix", transformComponent.Matrix);
            shaderComponent.SetVariable("fogNear", 0.02f);
            shaderComponent.SetVariable("skyColor", new Vector3f(100 / 255f, 149 / 255f, 237 / 255f)); // Cornflower blue

            entity.GetComponent<MaterialComponent>().BindAll(shaderComponent);

            SceneManager.Instance.lights[0].GetComponent<LightComponent>().Bind(shaderComponent);

            Gl.DrawArrays(PrimitiveType.Triangles, 0, meshComponent.RenderMesh.ElementCount * sizeof(float));

            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// Render all the entities within the render manager.
        /// </summary>
        public override void Run()
        {
            var sceneCamera = SceneManager.Instance.mainCamera;
            var sceneCameraComponent = sceneCamera.GetComponent<CameraComponent>();
            var sceneCameraTransform = sceneCamera.GetComponent<TransformComponent>();

            // Render shadows
            var mainLightEntity = SceneManager.Instance.lights[0];
            var mainLightComponent = mainLightEntity.GetComponent<LightComponent>();

            // Render scene from light (shadow map)
            mainLightComponent.shadowMap.Bind();
            RenderLights(mainLightComponent);
            mainLightComponent.shadowMap.Unbind();

            // Render scene
            renderer.PrepareRender();
            mainFramebuffer.Bind();
            renderer.PrepareFramebufferRender();

            RenderScene(sceneCameraComponent.projMatrix, sceneCameraComponent.viewMatrix, sceneCameraTransform.Position);

            // Render shadow map to display
            if (RenderShadowMap)
                mainLightComponent.shadowMap.Render();

            mainFramebuffer.Render();

            renderer.FinishRender();
            
            RenderCef();

            LastFrameTime = (DateTime.Now - lastRender).Milliseconds;

            if (!Paused)
            {
                FrametimeHistory[currentFrametimeIndex++] = LastFrameTime;

                if (currentFrametimeIndex == FrametimeHistory.Length)
                {
                    currentFrametimeIndex--;
                    for (var i = 0; i < FrametimeHistory.Length; ++i)
                        FrametimeHistory[i] = FrametimeHistory[(i + 1) % FrametimeHistory.Length];
                }

                FramerateHistory[currentFramerateIndex++] = CalculatedFramerate;
                if (currentFramerateIndex == FramerateHistory.Length)
                {
                    currentFramerateIndex--;
                    for (var i = 0; i < FramerateHistory.Length; ++i)
                        FramerateHistory[i] = FramerateHistory[(i + 1) % FramerateHistory.Length];
                }
            }

            lastRender = DateTime.Now;
            // Are we rendering too fast?
            if (LastFrameTime < (1000f / GameSettings.FramerateLimit) && GameSettings.FramerateLimit > 0)
            {
                // TODO: is there anything more elegant for this?
                Thread.Sleep((int)Math.Ceiling((1000f / GameSettings.FramerateLimit) - LastFrameTime));
            }
        }
    }
}
