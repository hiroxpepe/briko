// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Briko.Editor.Internal;
using NUnit.Framework;

namespace Briko.Tests.Internal {

    /// <summary>
    /// TDD tests for HierarchySorter pure logic —
    /// FloorDetector.IsFloorContainer and FloorDetector.RenumberVariants.
    /// Write these tests first (RED), then implement the methods (GREEN).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class HierarchySorterTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // IsFloorContainer

        [Test, Description("'1F' is a floor container name")]
        public void IsFloorContainer_1F_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorContainer(name: "1F"), Is.True);
        }

        [Test, Description("'2F' is a floor container name")]
        public void IsFloorContainer_2F_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorContainer(name: "2F"), Is.True);
        }

        [Test, Description("'3F' is a floor container name (multi-floor support)")]
        public void IsFloorContainer_3F_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorContainer(name: "3F"), Is.True);
        }

        [Test, Description("'B1F' is a floor container name")]
        public void IsFloorContainer_B1F_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorContainer(name: "B1F"), Is.True);
        }

        [Test, Description("'B2F' is a floor container name")]
        public void IsFloorContainer_B2F_ReturnsTrue() {
            Assert.That(FloorDetector.IsFloorContainer(name: "B2F"), Is.True);
        }

        [Test, Description("'grounds' is not a floor container name")]
        public void IsFloorContainer_Grounds_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorContainer(name: "grounds"), Is.False);
        }

        [Test, Description("'grounds_1f' is not a floor container name")]
        public void IsFloorContainer_GroundsUnderscore_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorContainer(name: "grounds_1f"), Is.False);
        }

        [Test, Description("'blocks' is not a floor container name")]
        public void IsFloorContainer_Blocks_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorContainer(name: "blocks"), Is.False);
        }

        [Test, Description("'Platform' is not a floor container name")]
        public void IsFloorContainer_Platform_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorContainer(name: "Platform"), Is.False);
        }

        [Test, Description("'1f' lowercase is not a floor container name (spec uses uppercase only)")]
        public void IsFloorContainer_LowercaseF_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorContainer(name: "1f"), Is.False);
        }

        [Test, Description("Empty string is not a floor container name")]
        public void IsFloorContainer_EmptyString_ReturnsFalse() {
            Assert.That(FloorDetector.IsFloorContainer(name: ""), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // RenumberVariants

        [Test, Description("Single item gets variant suffix _1")]
        public void RenumberVariants_SingleItem_ReturnsVariant1() {
            var items = new List<(string base_name, float x, float z)> {
                ("Ground_10.0x0.5x10.0_Green", 0f, 0f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("Ground_10.0x0.5x10.0_Green_1"));
        }

        [Test, Description("Two items with different Z are sorted Z ascending")]
        public void RenumberVariants_TwoItemsDifferentZ_SortsByZAscending() {
            var items = new List<(string base_name, float x, float z)> {
                ("Ground_10.0x0.5x10.0_Green", 0f, 10f),
                ("Ground_10.0x0.5x10.0_Green", 0f,  0f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Ground_10.0x0.5x10.0_Green_1"));
            Assert.That(result[1], Is.EqualTo("Ground_10.0x0.5x10.0_Green_2"));
        }

        [Test, Description("Two items with same Z are sorted X ascending")]
        public void RenumberVariants_TwoItemsSameZDifferentX_SortsByXAscending() {
            var items = new List<(string base_name, float x, float z)> {
                ("Ground_10.0x0.5x10.0_Green", 10f, 0f),
                ("Ground_10.0x0.5x10.0_Green",  0f, 0f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Ground_10.0x0.5x10.0_Green_1"));
            Assert.That(result[1], Is.EqualTo("Ground_10.0x0.5x10.0_Green_2"));
        }

        [Test, Description("Three items at (0,0),(10,0),(0,10) are numbered in Z-then-X order")]
        public void RenumberVariants_ThreeItems_NumbersSequentiallyByZThenX() {
            var items = new List<(string base_name, float x, float z)> {
                ("Ground_10.0x0.5x10.0_Green", 0f,  10f),
                ("Ground_10.0x0.5x10.0_Green", 10f,  0f),
                ("Ground_10.0x0.5x10.0_Green", 0f,   0f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Ground_10.0x0.5x10.0_Green_1"));
            Assert.That(result[1], Is.EqualTo("Ground_10.0x0.5x10.0_Green_2"));
            Assert.That(result[2], Is.EqualTo("Ground_10.0x0.5x10.0_Green_3"));
        }

        [Test, Description("Empty list returns empty result")]
        public void RenumberVariants_EmptyList_ReturnsEmpty() {
            var items = new List<(string base_name, float x, float z)>();
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test, Description("Mixed base names: each type gets its own _1 (not global sequential)")]
        public void RenumberVariants_MixedBaseNames_NumbersSequentially() {
            var items = new List<(string base_name, float x, float z)> {
                ("Ground_5.0x0.5x5.0_Blue",    0f, 10f),
                ("Ground_10.0x0.5x10.0_Green", 0f,  0f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Ground_10.0x0.5x10.0_Green_1"));
            Assert.That(result[1], Is.EqualTo("Ground_5.0x0.5x5.0_Blue_1"));
        }

        [Test, Description("Two different base names each start at _1 independently")]
        public void RenumberVariants_DifferentBasenames_EachStartAtOne() {
            var items = new List<(string base_name, float x, float z)> {
                ("Block_1.0x1.0x1.0_Plain_Green", 0f, 0f),
                ("Block_1.0x1.0x1.0_Green",       0f, 5f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green_1"));
            Assert.That(result[1], Is.EqualTo("Block_1.0x1.0x1.0_Green_1"));
        }

        [Test, Description("Interleaved base names: grouped output — all TypeA first, then TypeB")]
        public void RenumberVariants_InterleavedBasenames_NumberedPerType() {
            var items = new List<(string base_name, float x, float z)> {
                ("Block_1.0x1.0x1.0_Plain_Green", 0f,  0f),
                ("Block_1.0x1.0x1.0_Green",       0f,  5f),
                ("Block_1.0x1.0x1.0_Plain_Green", 0f, 10f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green_1"));
            Assert.That(result[1], Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green_2"));
            Assert.That(result[2], Is.EqualTo("Block_1.0x1.0x1.0_Green_1"));
        }

        [Test, Description("Level_1 style 3 interleaved types: grouped by first-appearance Z order")]
        public void RenumberVariants_InterleavedTypesOutputGrouped() {
            var items = new List<(string base_name, float x, float z)> {
                ("Block_1.0x1.0x1.0_Plain_Green", 0f, 2.5f),
                ("Block_0.5x0.5x0.5_Green",       0f, 3.25f),
                ("Block_1.0x1.0x1.0_Green",        0f, 4.0f),
                ("Block_1.0x1.0x1.0_Plain_Green",  0f, 6.75f)
            };
            List<string> result = FloorDetector.RenumberVariants(items: items);
            Assert.That(result[0], Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green_1"));
            Assert.That(result[1], Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green_2"));
            Assert.That(result[2], Is.EqualTo("Block_0.5x0.5x0.5_Green_1"));
            Assert.That(result[3], Is.EqualTo("Block_1.0x1.0x1.0_Green_1"));
        }

        ///////////////////////////////////////////////////////////////////////
        // IsStructuralContainer (Bug 1: re-run produces duplicate empty containers)

        [Test, Description("'1F' floor label is a structural container — always destroy on re-run")]
        public void IsStructuralContainer_FloorLabel_ReturnsTrue() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "1F"), Is.True);
        }

        [Test, Description("'B1F' basement floor label is a structural container")]
        public void IsStructuralContainer_BasementLabel_ReturnsTrue() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "B1F"), Is.True);
        }

        [Test, Description("'grounds' post-sort container is structural")]
        public void IsStructuralContainer_GroundsPostSort_ReturnsTrue() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "grounds"), Is.True);
        }

        [Test, Description("'grounds_1f' pre-sort container is structural")]
        public void IsStructuralContainer_GroundsPreSort_ReturnsTrue() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "grounds_1f"), Is.True);
        }

        [Test, Description("'blocks' post-sort container is structural")]
        public void IsStructuralContainer_BlocksPostSort_ReturnsTrue() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "blocks"), Is.True);
        }

        [Test, Description("'blocks_plain' pre-sort container is structural")]
        public void IsStructuralContainer_BlocksPreSort_ReturnsTrue() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "blocks_plain"), Is.True);
        }

        [Test, Description("'Platform' root is not a structural container — must never be destroyed")]
        public void IsStructuralContainer_PlatformRoot_ReturnsFalse() {
            Assert.That(FloorDetector.IsStructuralContainer(name: "Platform"), Is.False);
        }

        [Test, Description("A prefab instance name is not a structural container")]
        public void IsStructuralContainer_PrefabName_ReturnsFalse() {
            Assert.That(
                FloorDetector.IsStructuralContainer(name: "Ground_10.0x0.5x10.0_Green_1"),
                Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // IsVariantOrderValid (Bug 2: _2 displayed before _1 in Hierarchy)

        [Test, Description("Empty list is valid")]
        public void IsVariantOrderValid_EmptyList_ReturnsTrue() {
            var items = new List<(string base_name, int variant)>();
            Assert.That(FloorDetector.IsVariantOrderValid(items: items), Is.True);
        }

        [Test, Description("Single item with variant 1 is valid")]
        public void IsVariantOrderValid_SingleItem_ReturnsTrue() {
            var items = new List<(string base_name, int variant)> { ("Ground_Green", 1) };
            Assert.That(FloorDetector.IsVariantOrderValid(items: items), Is.True);
        }

        [Test, Description("Same type with variants 1 then 2 is valid")]
        public void IsVariantOrderValid_SameTypeAscending_ReturnsTrue() {
            var items = new List<(string base_name, int variant)> {
                ("Ground_Green", 1), ("Ground_Green", 2)
            };
            Assert.That(FloorDetector.IsVariantOrderValid(items: items), Is.True);
        }

        [Test, Description("Same type with variants 2 then 1 is invalid — the actual bug")]
        public void IsVariantOrderValid_SameTypeDescending_ReturnsFalse() {
            var items = new List<(string base_name, int variant)> {
                ("Ground_Green", 2), ("Ground_Green", 1)
            };
            Assert.That(FloorDetector.IsVariantOrderValid(items: items), Is.False);
        }

        [Test, Description("Mixed types all in order is valid — A_1, B_1, A_2")]
        public void IsVariantOrderValid_MixedTypesAllInOrder_ReturnsTrue() {
            var items = new List<(string base_name, int variant)> {
                ("Block_Plain_Green", 1), ("Block_Green", 1), ("Block_Plain_Green", 2)
            };
            Assert.That(FloorDetector.IsVariantOrderValid(items: items), Is.True);
        }

        [Test, Description("Mixed types with one type out of order is invalid — A_2, B_1, A_1")]
        public void IsVariantOrderValid_MixedTypesOutOfOrder_ReturnsFalse() {
            var items = new List<(string base_name, int variant)> {
                ("Block_Plain_Green", 2), ("Block_Green", 1), ("Block_Plain_Green", 1)
            };
            Assert.That(FloorDetector.IsVariantOrderValid(items: items), Is.False);
        }
    }
}
