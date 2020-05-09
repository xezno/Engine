﻿using ECSEngine.DebugUtils;
using ECSEngine.Events;
using ECSEngine.Managers;
using ECSEngine.Managers.Scripting;
using ECSEngine.MathUtils;
using ECSEngine.Render;
using ECSEngine.Types;
using Newtonsoft.Json;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace ECSEngine
{
    public class Game : IHasParent
    {
        #region Variables
        private readonly Gl.DebugProc debugCallback; // Stored to prevent GC from collecting debug callback before it can be called
        private readonly string gamePropertyPath;
        private readonly List<Thread> threads = new List<Thread>();

        private int titlebarHeight = 18;
        private List<IManager> mainThreadManagers = new List<IManager>();
        private GameProperties gameProperties;

        protected NativeWindow nativeWindow;
        private Vector2 lastMousePos;
        private bool ignoreSingleMouseInput;

        private Framebuffer mainFramebuffer;

        public bool isRunning = true; // TODO: properly detect window close event (needs adding within nativewindow)

        public IHasParent Parent { get; set; }
        public MouseMode MouseMode { get; set; } = MouseMode.Locked;
        #endregion

        #region Methods
        public Game(string gamePropertyPath)
        {
            this.gamePropertyPath = gamePropertyPath;
            debugCallback = DebugCallback;
        }

        public void RenderImGui() { }

        public void Run()
        {
            LoadGameProperties();
            InitNativeWindow();
        }

        private void Render(object sender, NativeWindowEventArgs e)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear non-fb
            mainFramebuffer.PreRender();
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear fb

            foreach (var manager in mainThreadManagers)
            {
                if (manager.GetType() != typeof(ImGuiManager))
                    manager.Run();
            }

            mainFramebuffer.Render();
            ImGuiManager.Instance.Run();

            Gl.Finish();
        }
        #endregion

        #region Initialization
        private void InitNativeWindow()
        {
            nativeWindow = NativeWindow.Create();

            nativeWindow.ContextCreated += ContextCreated;
            nativeWindow.ContextDestroying += ContextDestroyed;
            nativeWindow.Render += Render;
            nativeWindow.KeyDown += KeyDown;
            nativeWindow.KeyUp += KeyUp;
            nativeWindow.MouseDown += MouseDown;
            nativeWindow.MouseUp += MouseUp;
            nativeWindow.MouseMove += MouseMove;
            nativeWindow.MouseWheel += MouseWheel;
            nativeWindow.MouseLeave += NativeWindowOnMouseLeave;
            nativeWindow.MouseEnter += NativeWindowOnMouseEnter;

            nativeWindow.CursorVisible = true;
            nativeWindow.Animation = false; // Changing this to true makes input poll like once every 500ms. so don't change it
            nativeWindow.DepthBits = 24;
            nativeWindow.SwapInterval = 0;
            nativeWindow.Resize += Resize;
            nativeWindow.Create(GameSettings.Default.gamePosX, GameSettings.Default.gamePosY, GameSettings.Default.gameResolutionX + 16, GameSettings.Default.gameResolutionY + 16, NativeWindowStyle.Caption);

            // nativeWindow.SetCursorPos(new Point((int)(RenderSettings.Default.gamePosX + (RenderSettings.Default.gameResolutionX / 2)),
            //  (int)(RenderSettings.Default.gamePosY + (RenderSettings.Default.gameResolutionY / 2))));

            nativeWindow.Fullscreen = GameSettings.Default.fullscreen;
            nativeWindow.Caption = FilterString(gameProperties.WindowTitle) ?? "ECSEngine Game";

            // TODO: get choice of monitor to use.

            nativeWindow.Show();
            nativeWindow.Run();
            nativeWindow.Destroy();
        }

        private void NativeWindowOnMouseEnter(object sender, NativeWindowMouseEventArgs e)
        {
            ignoreSingleMouseInput = true;
        }

        private void NativeWindowOnMouseLeave(object sender, NativeWindowEventArgs e)
        {
            ignoreSingleMouseInput = true;
        }

        private string FilterString(string str)
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            str = str.Replace("{Version}", version?.ToString())
                .Replace("{Build}", version?.Build.ToString())
                .Replace("{Revision}", version?.Revision.ToString());
            return str;
        }

        private void LoadGameProperties()
        {
            using var streamReader = new StreamReader(gamePropertyPath);
            gameProperties = JsonConvert.DeserializeObject<GameProperties>(streamReader.ReadToEnd());
        }

        protected virtual void InitSystems()
        {
            mainThreadManagers = new List<IManager> {
                RenderManager.Instance,
                ImGuiManager.Instance
            };

            foreach (var mainThreadManager in mainThreadManagers)
            {
                EventManager.AddManager(mainThreadManager);
                mainThreadManager.Parent = this;
            }

            var multiThreadedManagers = new List<IManager>
            {
                UpdateManager.Instance,
                SceneManager.Instance,
                ScriptManager.Instance,
                RconManager.Instance,
                RconWebFrontendManager.Instance,
            };

            foreach (var multiThreadedManager in multiThreadedManagers)
            {
                EventManager.AddManager(multiThreadedManager);
                multiThreadedManager.Parent = this;

                threads.Add(new Thread(() =>
                {
                    while (isRunning)
                    {
                        multiThreadedManager.Run();
                    }
                }));
            }
        }

        protected virtual void InitScene() { }

        protected virtual void StartThreads()
        {
            foreach (var thread in threads)
            {
                thread.Start();
            }
        }

        private void LoadContent() { }
        #endregion

        #region Event Handlers
        private void ContextCreated(object sender, NativeWindowEventArgs e)
        {
            Logging.Log($"OpenGL {Gl.GetString(StringName.Version)}");
            CheckHardwareCompatibility();
            Gl.ReadBuffer(ReadBufferMode.Back);
            Gl.Enable(EnableCap.Blend);
            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Gl.DebugMessageCallback(debugCallback, IntPtr.Zero);

            InitSystems();
            InitScene();
            LoadContent();

            mainFramebuffer = new Framebuffer();

            // Setup complete - broadcast the game started event
            EventManager.BroadcastEvent(Event.GameStart, new GenericEventArgs(this));
            StartThreads();
        }

        private void Resize(object sender, EventArgs e)
        {
            var windowSize = new Vector2(nativeWindow.ClientSize.Width, nativeWindow.ClientSize.Height);

            Gl.Viewport(0, 0, nativeWindow.ClientSize.Width, nativeWindow.ClientSize.Height);

            EventManager.BroadcastEvent(Event.WindowResized, new WindowResizeEventArgs(windowSize, this));
        }

        // TODO: Fix mouse wheel
        private void MouseWheel(object sender, NativeWindowMouseEventArgs e)
        {
            EventManager.BroadcastEvent(Event.MouseScroll, new MouseWheelEventArgs(e.WheelTicks, this));
            //Logging.Log($"Scrolled by {e.WheelTicks} ticks");
        }

        // For some reason this offsets by the titlebar height, and it's inverted, so we have to do some quick maths to fix that
        private void MouseMove(object sender, NativeWindowMouseEventArgs e)
        {
            // TODO: Fix mouse positioning
            var mousePos = new Vector2(e.Location.X + 16,
                GameSettings.Default.gameResolutionY - e.Location.Y -
                (GameSettings.Default.fullscreen ? 0 : titlebarHeight) + 16);

            var mouseDelta = lastMousePos - mousePos;

            if (ignoreSingleMouseInput)
            {
                mouseDelta = new Vector2(0, 0);
                ignoreSingleMouseInput = false;
            }

            EventManager.BroadcastEvent(Event.MouseMove,
                new MouseMoveEventArgs(mouseDelta, mousePos,
                    this));

            lastMousePos = mousePos;


            //if (MouseMode == MouseMode.Locked)
            //    nativeWindow.SetCursorPos(new Point(0, 0));
        }

        private void MouseUp(object sender, NativeWindowMouseEventArgs e)
        {
            var button = 0;
            if ((e.Buttons & MouseButton.Left) != 0) button = 0;
            else if ((e.Buttons & MouseButton.Middle) != 0) button = 1;
            else if ((e.Buttons & MouseButton.Right) != 0) button = 2;

            EventManager.BroadcastEvent(Event.MouseButtonUp, new MouseButtonEventArgs(button, this));
        }

        private void MouseDown(object sender, NativeWindowMouseEventArgs e)
        {
            var button = 0;
            if ((e.Buttons & MouseButton.Left) != 0) button = 0;
            else if ((e.Buttons & MouseButton.Middle) != 0) button = 1;
            else if ((e.Buttons & MouseButton.Right) != 0) button = 2;

            EventManager.BroadcastEvent(Event.MouseButtonDown, new MouseButtonEventArgs(button, this));
        }

        private void KeyUp(object sender, NativeWindowKeyEventArgs e) => EventManager.BroadcastEvent(Event.KeyUp, new KeyboardEventArgs((int)e.Key, this));

        private void KeyDown(object sender, NativeWindowKeyEventArgs e) => EventManager.BroadcastEvent(Event.KeyDown, new KeyboardEventArgs((int)e.Key, this));

        private void ContextDestroyed(object sender, NativeWindowEventArgs e)
        {
            isRunning = false;
        }

        private void DebugCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (severity >= DebugSeverity.DebugSeverityMedium)
                DebugUtils.Logging.Log($"OpenGL Error {id}: {Marshal.PtrToStringAnsi(message, length)}", DebugUtils.Logging.Severity.Fatal);
        }

        private void CheckHardwareCompatibility()
        {
            var requiredExtensions = new[] { "GL_ARB_spirv_extensions" };
            var existingExtensions = new List<String>();

            var extensionCount = 0;
            Gl.GetInteger(GetPName.NumExtensions, out extensionCount);

            for (int i = 0; i < extensionCount; ++i)
            {
                existingExtensions.Add(Gl.GetString(StringName.Extensions, (uint)i));
            }

            foreach (var extension in requiredExtensions)
            {
                if (!existingExtensions.Contains(extension))
                {
                    Logging.Log($"GPU does not support extension {extension}", Logging.Severity.Fatal);
                }
            }
        }
        #endregion
    }
}
