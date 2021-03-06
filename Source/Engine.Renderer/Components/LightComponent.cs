﻿using Engine.ECS.Components;
using Engine.Utils.Attributes;
using OpenGL;

namespace Engine.Renderer.Components
{
    [Requires(typeof(TransformComponent))]
    public sealed class LightComponent : Component<LightComponent>
    {
        private Matrix4x4f viewMatrix;

        public LightComponent()
        {
            var size = 20f;
            var farPlane = 200f;
            ProjMatrix = Matrix4x4f.Ortho(-size, size, -size, size, 0.1f, farPlane);
            ViewMatrix = Matrix4x4f.Identity;
            ShadowMap = new ShadowMap(4096, 4096);
        }

        public Matrix4x4f ViewMatrix { get => viewMatrix; set => viewMatrix = value; }
        public Matrix4x4f ProjMatrix { get; set; }
        public ShadowMap ShadowMap { get; set; }

        public override void Update(float deltaTime)
        {
            var transformComponent = GetComponent<TransformComponent>();
            viewMatrix = Matrix4x4f.Identity;
            viewMatrix.RotateX((float)transformComponent.RotationEuler.X);
            viewMatrix.RotateY((float)transformComponent.RotationEuler.Y);
            viewMatrix.RotateZ((float)transformComponent.RotationEuler.Z);
            viewMatrix *= Matrix4x4f.LookAtDirection(new Vertex3f(
                (float)transformComponent.Position.X,
                (float)transformComponent.Position.Y,
                (float)transformComponent.Position.Z), new Vertex3f(0f, 0f, -1f), new Vertex3f(0f, 1f, 0f));
        }
    }
}
