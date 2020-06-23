﻿using Engine.ECS.Notify;
using Engine.ECS.Managers;
using Engine.Gui.Managers;
using Engine.Managers;
using Engine.Renderer.GL.Managers;
using Engine.Types;
using Engine.Utils;
using Engine.Utils.MathUtils;
using Newtonsoft.Json;
using OpenGL.CoreUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Engine
{
    public class Game : IHasParent
    {
        #region Variables
        private readonly string gamePropertyPath;
        private readonly List<Thread> threads = new List<Thread>();

        private int titlebarHeight = 18;
        private List<IManager> mainThreadManagers = new List<IManager>();
        private GameProperties gameProperties;

        protected NativeWindow nativeWindow;
        private Vector2 lastMousePos;
        private bool ignoreSingleMouseInput;

        public bool isRunning = true; // TODO: properly detect window close event (needs adding within nativewindow)

        public IHasParent Parent { get; set; }
        public MouseMode MouseMode { get; set; } = MouseMode.Locked;
        #endregion

        #region Methods
        public Game(string gamePropertyPath)
        {
            this.gamePropertyPath = gamePropertyPath;
        }

        public void RenderImGui() { }

        public void Run()
        {
            GameSettings.LoadValues();

            LoadGameProperties();
            InitNativeWindow();
        }

        private void Render(object sender, NativeWindowEventArgs e)
        {
            RenderManager.Instance.Run();
            ImGuiManager.Instance.Run();
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
            nativeWindow.Create(GameSettings.GamePosX, GameSettings.GamePosY, (uint)GameSettings.GameResolutionX + 16, (uint)GameSettings.GameResolutionY + 16, NativeWindowStyle.Caption);

            nativeWindow.Fullscreen = GameSettings.Fullscreen;
            nativeWindow.Caption = FilterString(gameProperties.WindowTitle) ?? "Engine Game";

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
                Broadcast.AddManager(mainThreadManager);
                mainThreadManager.Parent = this;
            }

            var multiThreadedManagers = new List<IManager>
            {
                UpdateManager.Instance,
                PhysicsManager.Instance,
                SceneManager.Instance,
                // ScriptManager.Instance,
                RconManager.Instance,
                RconWebFrontendManager.Instance,
            };

            foreach (var multiThreadedManager in multiThreadedManagers)
            {
                Broadcast.AddManager(multiThreadedManager);
                multiThreadedManager.Parent = this;

                threads.Add(new Thread(() =>
                {
                    while (isRunning)
                    {
                        multiThreadedManager.Run();

                        // Only update once every frame. Prevents multi-frame updating, but might break physics somewhere down the line
                        Thread.Sleep((int)(GameSettings.UpdateTimeStep * 1000f));
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
            InitSystems();
            InitScene();
            LoadContent();

            // Setup complete - broadcast the game started event
            Broadcast.Notify(NotifyType.GameStart, new GenericNotifyArgs(this));
            StartThreads();
        }

        private void Resize(object sender, EventArgs e)
        {
            var windowSize = new Vector2(nativeWindow.ClientSize.Width, nativeWindow.ClientSize.Height);

            // renderer.SetViewportSize();

            Broadcast.Notify(NotifyType.WindowResized, new WindowResizeNotifyArgs(windowSize, this));
        }

        // TODO: Fix mouse wheel
        private void MouseWheel(object sender, NativeWindowMouseEventArgs e)
        {
            Broadcast.Notify(NotifyType.MouseScroll, new MouseWheelNotifyArgs(e.WheelTicks, this));
            //Logging.Log($"Scrolled by {e.WheelTicks} ticks");
        }

        // For some reason this offsets by the titlebar height, and it's inverted, so we have to do some quick maths to fix that
        private void MouseMove(object sender, NativeWindowMouseEventArgs e)
        {
            // TODO: Fix mouse positioning
            var mousePos = new Vector2(e.Location.X, GameSettings.GameResolutionY - e.Location.Y - 5); // TODO: Fix this weird offset

            var mouseDelta = lastMousePos - mousePos;

            if (ignoreSingleMouseInput)
            {
                mouseDelta = new Vector2(0, 0);
                ignoreSingleMouseInput = false;
            }

            Broadcast.Notify(NotifyType.MouseMove,
                new MouseMoveNotifyArgs(mouseDelta, mousePos,
                    this));

            lastMousePos = mousePos;

            //if (MouseMode == MouseMode.Locked)
            //    nativeWindow.SetCursorPos(new Point((int)(GameSettings.GamePosX + (GameSettings.GameResolutionX / 2)),
            //        (int)(GameSettings.GamePosY + (GameSettings.GameResolutionY / 2))));
        }

        private void MouseUp(object sender, NativeWindowMouseEventArgs e)
        {
            var button = 0;
            if ((e.Buttons & MouseButton.Left) != 0) button = 0;
            else if ((e.Buttons & MouseButton.Middle) != 0) button = 1;
            else if ((e.Buttons & MouseButton.Right) != 0) button = 2;

            Broadcast.Notify(NotifyType.MouseButtonUp, new MouseButtonNotifyArgs(button, this));
        }

        private void MouseDown(object sender, NativeWindowMouseEventArgs e)
        {
            var button = 0;
            if ((e.Buttons & MouseButton.Left) != 0) button = 0;
            else if ((e.Buttons & MouseButton.Middle) != 0) button = 1;
            else if ((e.Buttons & MouseButton.Right) != 0) button = 2;

            Broadcast.Notify(NotifyType.MouseButtonDown, new MouseButtonNotifyArgs(button, this));
        }

        private void KeyUp(object sender, NativeWindowKeyEventArgs e) => Broadcast.Notify(NotifyType.KeyUp, new KeyboardNotifyArgs((int)e.Key, this));

        private void KeyDown(object sender, NativeWindowKeyEventArgs e) => Broadcast.Notify(NotifyType.KeyDown, new KeyboardNotifyArgs((int)e.Key, this));

        private void ContextDestroyed(object sender, NativeWindowEventArgs e)
        {
            isRunning = false;
        }
        #endregion
    }
}