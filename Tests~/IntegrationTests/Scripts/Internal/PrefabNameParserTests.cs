// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

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
    }
}
