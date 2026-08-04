// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Briko.Editor.Internal;
using NUnit.Framework;

namespace Briko.Tests.Internal {

    /// <summary>
    /// TDD tests for FloorDetector — floor structure detection pure logic.
    /// Write these tests first (RED), then implement FloorDetector (GREEN).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class FloorDetectorTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // ParseDimensions

        [Test, Description("Parses X Y Z from a standard Ground prefab name")]
        public void ParseDimensions_GroundName_ReturnsXYZ() {
            var result = FloorDetector.ParseDimensions(name: "Ground_10.0x0.5x10.0_Green_1");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.x, Is.EqualTo(10.0f).Within(0.001f));
            Assert.That(result.Value.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(result.Value.z, Is.EqualTo(10.0f).Within(0.001f));
        }

        [Test, Description("Parses X Y Z from a small Ground prefab name")]
        public void ParseDimensions_SmallGround_ReturnsXYZ() {
            var result = FloorDetector.ParseDimensions(name: "Ground_2.5x0.5x2.5_Blue_3");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.x, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(result.Value.z, Is.EqualTo(2.5f).Within(0.001f));
        }

        [Test, Description("Parses X Y Z from a Block prefab name")]
        public void ParseDimensions_BlockName_ReturnsXYZ() {
            var result = FloorDetector.ParseDimensions(name: "Block_1.0x1.0x1.0_Plain_Green_1");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.x, Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(result.Value.y, Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(result.Value.z, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test, Description("Returns null for a name without dimension segment")]
        public void ParseDimensions_InvalidName_ReturnsNull() {
            var result = FloorDetector.ParseDimensions(name: "Ground_invalid");
            Assert.That(result, Is.Null);
        }

        ///////////////////////////////////////////////////////////////////////
        // IsFloorAnchor

        [Test, Description("10x10 Ground is a floor anchor")]
        public void IsFloorAnchor_TenByTen_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorAnchor(x: 10.0f, z: 10.0f), Is.True);
        }

        [Test, Description("5x5 Ground is a floor anchor (boundary)")]
        public void IsFloorAnchor_FiveByFive_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorAnchor(x: 5.0f, z: 5.0f), Is.True);
        }

        [Test, Description("2.5x2.5 Ground is not a floor anchor")]
        public void IsFloorAnchor_TwoPointFiveByTwoPointFive_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorAnchor(x: 2.5f, z: 2.5f), Is.False);
        }

        [Test, Description("1x1 Block is not a floor anchor")]
        public void IsFloorAnchor_OneByOne_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorAnchor(x: 1.0f, z: 1.0f), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // CalcSurfaceY

        [Test, Description("Surface Y of Ground at Y=-0.25 is 0.0 (1F ground level, center at -0.25, top at 0.0)")]
        public void CalcSurfaceY_PrefabAtMinusHalf_ReturnsZero() {
            float result = FloorDetector.CalcSurfaceY(prefab_y: -0.25f);
            Assert.That(result, Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test, Description("Surface Y of Ground at Y=1.0 is 1.25 (2F in Level_2)")]
        public void CalcSurfaceY_PrefabAtOne_ReturnsOnePointTwoFive() {
            float result = FloorDetector.CalcSurfaceY(prefab_y: 1.0f);
            Assert.That(result, Is.EqualTo(1.25f).Within(0.001f));
        }

        [Test, Description("Surface Y of Ground at Y=-5.5 is -5.25 (B2F in Level_2)")]
        public void CalcSurfaceY_PrefabAtMinusFivePointFive_ReturnsMinusFivePointTwoFive() {
            float result = FloorDetector.CalcSurfaceY(prefab_y: -5.5f);
            Assert.That(result, Is.EqualTo(-5.25f).Within(0.001f));
        }

        ///////////////////////////////////////////////////////////////////////
        // AssignFloorLabels

        [Test, Description("Level_2 surfaces produce 4 floors: 2F / 1F / B1F / B2F")]
        public void AssignFloorLabels_Level2Surfaces_ReturnsFourFloors() {
            var surfaces = new List<float> { 1.25f, -0.25f, -2.75f, -5.25f };
            var result = FloorDetector.AssignFloorLabels(surface_y_values_desc: surfaces);
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result[0].label, Is.EqualTo("2F"));
            Assert.That(result[1].label, Is.EqualTo("1F"));
            Assert.That(result[2].label, Is.EqualTo("B1F"));
            Assert.That(result[3].label, Is.EqualTo("B2F"));
        }

        [Test, Description("Single surface at Y=0 is assigned 1F")]
        public void AssignFloorLabels_SingleSurfaceAtZero_Returns1F() {
            var surfaces = new List<float> { 0.0f };
            var result = FloorDetector.AssignFloorLabels(surface_y_values_desc: surfaces);
            Assert.That(result[0].label, Is.EqualTo("1F"));
        }

        [Test, Description("Two surfaces above and below zero produce 2F and 1F")]
        public void AssignFloorLabels_TwoSurfaces_Returns2FAnd1F() {
            var surfaces = new List<float> { 1.25f, -0.25f };
            var result = FloorDetector.AssignFloorLabels(surface_y_values_desc: surfaces);
            Assert.That(result[0].label, Is.EqualTo("2F"));
            Assert.That(result[1].label, Is.EqualTo("1F"));
        }

        [Test, Description("Empty list returns empty result")]
        public void AssignFloorLabels_EmptyList_ReturnsEmpty() {
            var result = FloorDetector.AssignFloorLabels(
                surface_y_values_desc: new List<float>());
            Assert.That(result.Count, Is.EqualTo(0));
        }

        ///////////////////////////////////////////////////////////////////////
        // AssignBlockToFloor

        [Test, Description("Block at Y=0.0 is within 1.4m of 1F surface at Y=-0.25")]
        public void AssignBlockToFloor_BlockOnFirstFloor_Returns1F() {
            var floors = new List<(float, string)> {
                (1.25f, "2F"), (-0.25f, "1F"), (-2.75f, "B1F"), (-5.25f, "B2F")
            };
            string result = FloorDetector.AssignBlockToFloor(
                block_y: 0.0f, floors_desc: floors);
            Assert.That(result, Is.EqualTo("1F"));
        }

        [Test, Description("Block at Y=1.5 is within 1.4m of 2F surface at Y=1.25")]
        public void AssignBlockToFloor_BlockOnSecondFloor_Returns2F() {
            var floors = new List<(float, string)> {
                (1.25f, "2F"), (-0.25f, "1F"), (-2.75f, "B1F"), (-5.25f, "B2F")
            };
            string result = FloorDetector.AssignBlockToFloor(
                block_y: 1.5f, floors_desc: floors);
            Assert.That(result, Is.EqualTo("2F"));
        }

        [Test, Description("Block at Y=-5.0 is within 1.4m of B2F surface at Y=-5.25")]
        public void AssignBlockToFloor_BlockOnB2F_ReturnsB2F() {
            var floors = new List<(float, string)> {
                (1.25f, "2F"), (-0.25f, "1F"), (-2.75f, "B1F"), (-5.25f, "B2F")
            };
            string result = FloorDetector.AssignBlockToFloor(
                block_y: -5.0f, floors_desc: floors);
            Assert.That(result, Is.EqualTo("B2F"));
        }

        ///////////////////////////////////////////////////////////////////////
        // IsDescending

        [Test, Description("Spawn above exit means descending level")]
        public void IsDescending_SpawnAboveExit_ReturnsTrue() {
            Assert.That(
                FloorDetector.IsDescending(spawn_y: 0.0f, exit_y: -5.0f),
                Is.True);
        }

        [Test, Description("Spawn below exit means ascending level")]
        public void IsDescending_SpawnBelowExit_ReturnsFalse() {
            Assert.That(
                FloorDetector.IsDescending(spawn_y: -5.0f, exit_y: 0.0f),
                Is.False);
        }

        [Test, Description("Spawn and exit at same Y defaults to non-descending")]
        public void IsDescending_SpawnEqualsExit_ReturnsFalse() {
            Assert.That(
                FloorDetector.IsDescending(spawn_y: 0.0f, exit_y: 0.0f),
                Is.False);
        }
        ///////////////////////////////////////////////////////////////////////
        // AssignFloorLabels — Level_3 pattern (all floors above ground)

        [Test, Description("Level_3 surfaces produce 4F/3F/2F/1F (all floors above ground, no basement)")]
        public void AssignFloorLabels_Level3Surfaces_ReturnsFourAboveGroundFloors() {
            var surfaces = new List<float> { 4.75f, 2.25f, 1.25f, -0.25f };
            var result = FloorDetector.AssignFloorLabels(surface_y_values_desc: surfaces);
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result[0].label, Is.EqualTo("4F"));
            Assert.That(result[1].label, Is.EqualTo("3F"));
            Assert.That(result[2].label, Is.EqualTo("2F"));
            Assert.That(result[3].label, Is.EqualTo("1F"));
        }

        ///////////////////////////////////////////////////////////////////////
        // AssignBlockToFloor — fallback path (Level_3: block at Y=7.0 above 4F)

        [Test, Description("Block at Y=7.0 is 2.25m above 4F surface (>1.4m) — fallback assigns to nearest floor below")]
        public void AssignBlockToFloor_BlockAboveCharacterHeight_FallsBackToNearestFloorBelow() {
            var floors = new List<(float, string)> {
                (4.75f, "4F"), (2.25f, "3F"), (1.25f, "2F"), (-0.25f, "1F")
            };
            string result = FloorDetector.AssignBlockToFloor(
                block_y: 7.0f, floors_desc: floors);
            Assert.That(result, Is.EqualTo("4F"));
        }
    }
}
