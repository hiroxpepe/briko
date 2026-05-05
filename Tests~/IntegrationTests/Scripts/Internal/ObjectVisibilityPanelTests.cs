// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

using Briko.Editor.Internal;
using NUnit.Framework;

namespace Briko.Tests.Internal {

    /// <summary>
    /// TDD tests for ObjectVisibilityPanel pure logic —
    /// PrefabNameParser.ParseKind, FloorDetector.IsGroundsContainer,
    /// and FloorDetector.IsBlocksContainer.
    /// Write these tests first (RED), then implement the methods (GREEN).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ObjectVisibilityPanelTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // PrefabNameParser.ParseKind

        [Test, Description("Extracts 'Ground' Kind from a standard ground prefab name")]
        public void ParseKind_GroundName_ReturnsGround() {
            string? result = PrefabNameParser.ParseKind(name: "Ground_10.0x0.5x10.0_Green_1");
            Assert.That(result, Is.EqualTo("Ground"));
        }

        [Test, Description("Extracts 'Block' Kind from a block prefab name")]
        public void ParseKind_BlockName_ReturnsBlock() {
            string? result = PrefabNameParser.ParseKind(name: "Block_1.0x1.0x1.0_Plain_Green_3");
            Assert.That(result, Is.EqualTo("Block"));
        }

        [Test, Description("Extracts 'Enemy' Kind from an enemy prefab name (future type)")]
        public void ParseKind_EnemyName_ReturnsEnemy() {
            string? result = PrefabNameParser.ParseKind(name: "Enemy_1.0x2.0x1.0_Red_2");
            Assert.That(result, Is.EqualTo("Enemy"));
        }

        [Test, Description("Extracts multi-word Kind 'Bipyramid' from prefab name")]
        public void ParseKind_BipyramidName_ReturnsBipyramid() {
            string? result = PrefabNameParser.ParseKind(name: "Bipyramid_0.5x1.0x0.5_Plain_Blue_1");
            Assert.That(result, Is.EqualTo("Bipyramid"));
        }

        [Test, Description("Returns null for a name without a valid dimension segment")]
        public void ParseKind_InvalidName_ReturnsNull() {
            string? result = PrefabNameParser.ParseKind(name: "Ground_invalid");
            Assert.That(result, Is.Null);
        }

        [Test, Description("Zone name 'vol_spawn' has no dimension segment — returns null to prevent spurious UI row")]
        public void ParseKind_ZoneName_ReturnsNull() {
            string? result = PrefabNameParser.ParseKind(name: "vol_spawn");
            Assert.That(result, Is.Null);
        }

        [Test, Description("Empty string has no dimension segment — returns null")]
        public void ParseKind_EmptyString_ReturnsNull() {
            string? result = PrefabNameParser.ParseKind(name: "");
            Assert.That(result, Is.Null);
        }

        ///////////////////////////////////////////////////////////////////////
        // FloorDetector.IsGroundsContainer

        [Test, Description("'grounds' (post-sort exact name) is a grounds container")]
        public void IsGroundsContainer_Grounds_ReturnsTrue() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "grounds"), Is.True);
        }

        [Test, Description("'grounds_1f' (pre-sort prefix name) is a grounds container")]
        public void IsGroundsContainer_Grounds1f_ReturnsTrue() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "grounds_1f"), Is.True);
        }

        [Test, Description("'grounds_2f' (pre-sort prefix name) is a grounds container")]
        public void IsGroundsContainer_Grounds2f_ReturnsTrue() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "grounds_2f"), Is.True);
        }

        [Test, Description("'blocks' is not a grounds container")]
        public void IsGroundsContainer_Blocks_ReturnsFalse() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "blocks"), Is.False);
        }

        [Test, Description("'1F' floor container is not a grounds container")]
        public void IsGroundsContainer_FloorLabel_ReturnsFalse() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "1F"), Is.False);
        }

        [Test, Description("'Grounds' uppercase is not a grounds container (spec requires lowercase)")]
        public void IsGroundsContainer_UppercaseG_ReturnsFalse() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "Grounds"), Is.False);
        }

        [Test, Description("'groundsX' without underscore separator is not a grounds container")]
        public void IsGroundsContainer_NoSeparator_ReturnsFalse() {
            Assert.That(FloorDetector.IsGroundsContainer(name: "groundsX"), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // FloorDetector.IsBlocksContainer

        [Test, Description("'blocks' (post-sort exact name) is a blocks container")]
        public void IsBlocksContainer_Blocks_ReturnsTrue() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "blocks"), Is.True);
        }

        [Test, Description("'blocks_plain' (pre-sort prefix name) is a blocks container")]
        public void IsBlocksContainer_BlocksPlain_ReturnsTrue() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "blocks_plain"), Is.True);
        }

        [Test, Description("'blocks_basic' (pre-sort prefix name) is a blocks container")]
        public void IsBlocksContainer_BlocksBasic_ReturnsTrue() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "blocks_basic"), Is.True);
        }

        [Test, Description("'grounds' is not a blocks container")]
        public void IsBlocksContainer_Grounds_ReturnsFalse() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "grounds"), Is.False);
        }

        [Test, Description("'1F' floor container is not a blocks container")]
        public void IsBlocksContainer_FloorLabel_ReturnsFalse() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "1F"), Is.False);
        }

        [Test, Description("'Blocks' uppercase is not a blocks container (spec requires lowercase)")]
        public void IsBlocksContainer_UppercaseB_ReturnsFalse() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "Blocks"), Is.False);
        }

        [Test, Description("'blocksX' without underscore separator is not a blocks container")]
        public void IsBlocksContainer_NoSeparator_ReturnsFalse() {
            Assert.That(FloorDetector.IsBlocksContainer(name: "blocksX"), Is.False);
        }
    }
}
