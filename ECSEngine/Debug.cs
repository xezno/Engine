﻿using System;
using System.Diagnostics;
using System.Reflection;

namespace ECSEngine
{
    public static class Debug
    {
        public enum DebugSeverity
        {
            Low,
            Medium,
            High,
            Fatal
        };

        public static void Log(string str, DebugSeverity severity = DebugSeverity.Low)
        {
            // Prepare method name & method class name
            StackTrace stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();
            MethodBase method = stackFrames[1].GetMethod();

            /* BUG: Some functions (namely ogl's debug callback) run on a separate thread, so 
             * they mess with the console's foreground color before another thread has finished outputting.
             * (pls fix) */

            switch (severity)
            {
                case DebugSeverity.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DebugSeverity.High:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case DebugSeverity.Low:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case DebugSeverity.Medium:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
            }
            Console.WriteLine($"[{DateTime.Now.ToString()}] {method.ReflectedType.Name}, {method.Name} ({severity}): {str}");
        }
    }
}
