// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.IO;
using UnityEngine;

namespace Briko.Editor {

    /// <summary>
    /// Lightweight file-based logger for Briko diagnostics.
    /// Writes to game/briko.log (project root) and mirrors to Unity Console.
    /// Usage: BrikoLog.Write("[Briko] message");
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class BrikoLog {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Fields

        /// <summary>Enable / disable the logger globally.</summary>
        public static bool Enabled = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Fields

        /// <summary>Cached log file path (relative to project root: game/briko.log).</summary>
        static string? _path;

        /// <summary>True if the file has been cleared at app startup.</summary>
        static bool _initialized = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Writes a timestamped message to game/briko.log and Unity Console.
        /// First call clears any previous log file.
        /// </summary>
        public static void Write(string message) {
            if (!Enabled) { return; }
            try {
                if (_path == null) {
                    _path = Path.Combine(Application.dataPath, "..", "briko.log");
                }
                if (!_initialized) {
                    File.WriteAllText(path: _path, contents: $"=== Briko diagnostic log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                    _initialized = true;
                }
                File.AppendAllText(path: _path, contents: $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            } catch (Exception ex) {
                Debug.LogError(message: $"[BrikoLog] write failed: {ex.Message}");
            }
            Debug.Log(message: message);
        }
    }
}