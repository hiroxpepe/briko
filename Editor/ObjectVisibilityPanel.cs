// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Briko.Editor.Internal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Briko.Editor {

    /// <summary>
    /// Persistent EditorWindow that shows per-type object counts and
    /// allows toggling visibility by type (Ground, Block, Zone).
    /// Menu: Tools > Briko > Object Visibility
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class ObjectVisibilityPanel : EditorWindow {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Private constants

        const string MENU_ROOT = "Tools/Briko/";
        const string PLATFORM_NAME = "Platform";
        const string ENTITY_NAME = "Entity";
        const string KIND_GROUND = "Ground";
        const string KIND_BLOCK = "Block";
        const string KIND_ZONE = "Zone";
        const string ZONE_PREFIX = "vol_";

        ///////////////////////////////////////////////////////////////////////
        // Private fields

        List<(string kind, List<GameObject> containers)> _type_entries = new();
        bool _needs_scan = true;

        ///////////////////////////////////////////////////////////////////////
        // Menu items

        /// <summary>
        /// Opens the Object Visibility panel as a dockable EditorWindow.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        [MenuItem(MENU_ROOT + "Object Visibility")]
        public static void ShowWindow() {
            GetWindow<ObjectVisibilityPanel>(title: "Briko — Object Visibility");
        }

        ///////////////////////////////////////////////////////////////////////
        // Unity EditorWindow lifecycle

        void OnEnable() {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            _needs_scan = true;
        }

        void OnDisable() {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            _needs_scan = true;
            Repaint();
        }

        ///////////////////////////////////////////////////////////////////////
        // GUI

        void OnGUI() {
            if (_needs_scan) {
                ScanScene();
                _needs_scan = false;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(text: "Refresh")) { ScanScene(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (_type_entries.Count == 0) {
                EditorGUILayout.HelpBox(
                    message: "Platform root not found or no objects detected.",
                    type: MessageType.Warning);
            }
            foreach (var entry in _type_entries) {
                int count = CountInstances(containers: entry.containers);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(text: entry.kind, options: GUILayout.Width(70));
                GUILayout.Label(text: $"[{count}]", options: GUILayout.Width(40));
                if (GUILayout.Button(text: "Show", options: GUILayout.Width(50))) {
                    SetContainersActive(containers: entry.containers, active: true);
                }
                if (GUILayout.Button(text: "Hide", options: GUILayout.Width(50))) {
                    SetContainersActive(containers: entry.containers, active: false);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label: "─────────────────────────────────");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(text: "Show All")) { SetAllActive(active: true); }
            if (GUILayout.Button(text: "Hide All")) { SetAllActive(active: false); }
            EditorGUILayout.EndHorizontal();
        }

        ///////////////////////////////////////////////////////////////////////
        // Private methods [verb, verb phrase]

        /// <summary>
        /// Rescans the active scene and rebuilds the type-to-containers map.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        void ScanScene() {
            _type_entries = new List<(string, List<GameObject>)>();
            GameObject? platform_root = FindRootObject(name: PLATFORM_NAME);
            if (platform_root != null) {
                ScanPlatformHierarchy(
                    parent: platform_root.transform,
                    entries: _type_entries);
            }
            GameObject? entity_root = FindRootObject(name: ENTITY_NAME);
            if (entity_root != null) {
                ScanEntityForZones(
                    entity_root: entity_root.transform,
                    entries: _type_entries);
            }
        }

        /// <summary>
        /// Recursively scans Platform children to collect grounds and blocks containers.
        /// Recurses into floor containers (1F, 2F, B1F, ...).
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void ScanPlatformHierarchy(
            Transform parent,
            List<(string kind, List<GameObject> containers)> entries) {

            foreach (Transform child in parent) {
                string name = child.name;
                if (FloorDetector.IsGroundsContainer(name: name)) {
                    AddContainerToEntries(
                        entries: entries, kind: KIND_GROUND, container: child.gameObject);
                } else if (FloorDetector.IsBlocksContainer(name: name)) {
                    AddContainerToEntries(
                        entries: entries, kind: KIND_BLOCK, container: child.gameObject);
                } else if (FloorDetector.IsFloorContainer(name: name)) {
                    ScanPlatformHierarchy(parent: child, entries: entries);
                }
            }
        }

        /// <summary>
        /// Scans Entity children for vol_* zone GameObjects.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void ScanEntityForZones(
            Transform entity_root,
            List<(string kind, List<GameObject> containers)> entries) {

            foreach (Transform child in entity_root) {
                if (child.name.StartsWith(ZONE_PREFIX)) {
                    AddContainerToEntries(
                        entries: entries, kind: KIND_ZONE, container: child.gameObject);
                }
            }
        }

        /// <summary>
        /// Adds a container to the entries list under the given kind.
        /// Creates a new kind row if it does not already exist.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void AddContainerToEntries(
            List<(string kind, List<GameObject> containers)> entries,
            string kind,
            GameObject container) {

            for (int i = 0; i < entries.Count; i++) {
                if (entries[i].kind == kind) {
                    entries[i].containers.Add(container);
                    return;
                }
            }
            List<GameObject> new_list = new() { container };
            entries.Add((kind, new_list));
        }

        /// <summary>
        /// Returns the total number of direct children across all given containers.
        /// Hidden objects are counted regardless of visibility state.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static int CountInstances(List<GameObject> containers) {
            int count = 0;
            foreach (GameObject container in containers) {
                if (container != null) { count += container.transform.childCount; }
            }
            return count;
        }

        /// <summary>
        /// Sets all containers of one type active or inactive.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void SetContainersActive(List<GameObject> containers, bool active) {
            foreach (GameObject container in containers) {
                if (container != null) {
                    container.SetActive(active);
                    EditorUtility.SetDirty(container);
                }
            }
        }

        /// <summary>
        /// Sets all containers of all types active or inactive.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        void SetAllActive(bool active) {
            foreach (var entry in _type_entries) {
                SetContainersActive(containers: entry.containers, active: active);
            }
        }

        /// <summary>
        /// Finds a root-level GameObject in the active scene by name.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static GameObject? FindRootObject(string name) {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                if (root.name == name) { return root; }
            }
            return null;
        }
    }
}
