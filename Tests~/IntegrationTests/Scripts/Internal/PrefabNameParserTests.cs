// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Briko.Editor.Internal;
using NUnit.Framework;

namespace Briko.Tests.Internal {

    /// <summary>
    /// Tests for PrefabNameParser - parsing Briko naming convention.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class PrefabNameParserTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Test methods [TestedClass_Feature_ExpectedBehavior]

        [Test, Description("Parse returns prefab name and variant for standard ground name")]
        public void Parse_GroundName_ReturnsPrefabAndVariant() {
            var result = PrefabNameParser.Parse(name: "Ground_10.0x0.5x10.0_Green_1");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.prefab, Is.EqualTo("Ground_10.0x0.5x10.0_Green"));
            Assert.That(result.Value.variant, Is.EqualTo(1));
        }

        [Test, Description("Parse handles multi-word color suffix in block name")]
        public void Parse_BlockWithMultiWordColor_ReturnsPrefabAndVariant() {
            var result = PrefabNameParser.Parse(name: "Block_1.0x1.0x1.0_Plain_Green_3");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.prefab, Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green"));
            Assert.That(result.Value.variant, Is.EqualTo(3));
        }

        [Test, Description("Parse returns null for names not matching Briko convention")]
        public void Parse_InvalidName_ReturnsNull() {
            var result = PrefabNameParser.Parse(name: "Ground_invalid");
            Assert.That(result, Is.Null);
        }

        [Test, Description("Parse correctly reads two-digit variant numbers")]
        public void Parse_TwoDigitVariant_ParsesCorrectly() {
            var result = PrefabNameParser.Parse(name: "Block_1.0x1.0x1.0_Plain_Green_15");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.prefab, Is.EqualTo("Block_1.0x1.0x1.0_Plain_Green"));
            Assert.That(result.Value.variant, Is.EqualTo(15));
        }

        [Test, Description("Parse handles arbitrary type prefix such as Enemy or Wall")]
        public void Parse_ArbitraryType_ReturnsPrefabAndVariant() {
            var result = PrefabNameParser.Parse(name: "Enemy_1.0x2.0x1.0_Red_2");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.prefab, Is.EqualTo("Enemy_1.0x2.0x1.0_Red"));
            Assert.That(result.Value.variant, Is.EqualTo(2));
        }

        [Test, Description("Parse handles Bipyramid type with multi-word descriptor")]
        public void Parse_BipyramidType_ReturnsPrefabAndVariant() {
            var result = PrefabNameParser.Parse(name: "Bipyramid_0.5x1.0x0.5_Plain_Blue_1");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.prefab, Is.EqualTo("Bipyramid_0.5x1.0x0.5_Plain_Blue"));
            Assert.That(result.Value.variant, Is.EqualTo(1));
        }
    }
}