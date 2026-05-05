// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

using System.IO;
using Briko.Editor.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Briko.Tests.Model {

    /// <summary>
    /// Tests for Layout data model classes: Root, Platform, Item, Zone.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class LayoutTests {
#nullable enable

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        };

        ///////////////////////////////////////////////////////////////////////
        // Test methods [TestedClass_Feature_ExpectedBehavior]

        [Test, Description("Root.layout_id deserializes from snake_case JSON key")]
        public void Root_LayoutId_DeserializesFromJson() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.layout_id, Is.EqualTo("test_minimal"));
        }

        [Test, Description("Root.grid_unit deserializes to 0.25f")]
        public void Root_GridUnit_DeserializesToQuarter() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.grid_unit, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test, Description("Root.target_duration_sec deserializes to 180")]
        public void Root_TargetDurationSec_DeserializesTo180() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.target_duration_sec, Is.EqualTo(180));
        }

        [Test, Description("Platform.floor deserializes to '1f'")]
        public void Platform_Floor_DeserializesTo1f() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.platforms[0].floor, Is.EqualTo("1f"));
        }

        [Test, Description("Platform.grounds contains one item from fixture")]
        public void Platform_Grounds_ContainsOneItem() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.platforms[0].grounds, Has.Count.EqualTo(1));
        }

        [Test, Description("Item.prefab deserializes without trailing variant number")]
        public void Item_Prefab_DeserializesWithoutVariant() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.platforms[0].grounds[0].prefab,
                Is.EqualTo("Ground_10.0x0.5x10.0_Green"));
        }

        [Test, Description("Zone.zone_id deserializes from snake_case JSON key")]
        public void Zone_ZoneId_DeserializesFromJson() {
            string json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(json, _settings);
            Assert.That(root!.platforms[0].zones[0].zone_id, Is.EqualTo("vol_spawn"));
        }
    }
}
