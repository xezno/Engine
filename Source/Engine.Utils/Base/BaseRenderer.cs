﻿using Engine.Utils.DebugUtils;

namespace Engine.Utils.Base
{
    public interface BaseRenderer
    {
        void ContextCreated();
        void RenderImGui();
        void RenderToScreen();
        void RenderToShadowMap();
    }

    public class NullRenderer : BaseRenderer
    {
        public void ContextCreated()
        {
            Logging.Log("The null renderer is in use; therefore, no graphics will be displayed.", Logging.Severity.Medium);
        }

        public void RenderImGui()
        {
            
        }

        public void RenderToScreen()
        {
            
        }

        public void RenderToShadowMap()
        {
            
        }
    }
}
