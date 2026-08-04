// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Briko.Editor.Internal;
using NUnit.Framework;

namespace Briko.Tests.Internal {

    /// <summary>
    /// Tests for GridSnapper - 0.25m grid snapping.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class GridSnapperTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Test methods [TestedClass_Feature_ExpectedBehavior]

        [Test, Description("Snap returns original values for positions already on grid")]
        public void Snap_AlreadyOnGrid_ReturnsSameValue() {
            float[] result = GridSnapper.Snap(raw: new float[] { 0.0f, 0.25f, 0.5f }, grid_unit: 0.25f);
            Assert.That(result[0], Is.EqualTo(0.0f).Within(0.001f));
            Assert.That(result[1], Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(result[2], Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test, Description("Snap rounds up to next grid unit when above midpoint")]
        public void Snap_AboveMidpoint_RoundsUp() {
            float[] result = GridSnapper.Snap(raw: new float[] { 0.15f, 0.0f, 0.0f }, grid_unit: 0.25f);
            Assert.That(result[0], Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test, Description("Snap rounds down to previous grid unit when below midpoint")]
        public void Snap_BelowMidpoint_RoundsDown() {
            float[] result = GridSnapper.Snap(raw: new float[] { 0.1f, 0.0f, 0.0f }, grid_unit: 0.25f);
            Assert.That(result[0], Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test, Description("Snap handles negative values correctly")]
        public void Snap_NegativeValue_SnapsToNearestGrid() {
            float[] result = GridSnapper.Snap(raw: new float[] { -0.15f, 0.0f, 0.0f }, grid_unit: 0.25f);
            Assert.That(result[0], Is.EqualTo(-0.25f).Within(0.001f));
        }

        [Test, Description("Snap handles zero correctly")]
        public void Snap_Zero_ReturnsZero() {
            float[] result = GridSnapper.Snap(raw: new float[] { 0.0f, 0.0f, 0.0f }, grid_unit: 0.25f);
            Assert.That(result[0], Is.EqualTo(0.0f).Within(0.001f));
        }
    }
}
