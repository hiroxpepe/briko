// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using Briko.Editor.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Briko.Tests.Model {

    /// <summary>
    /// Tests for JSON to POCO round-trip fidelity.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class RoundTripTests {
#nullable enable

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        };

        ///////////////////////////////////////////////////////////////////////
        // Test methods [TestedClass_Feature_ExpectedBehavior]

        [Test, Description("Serializing and deserializing JSON produces identical JSON tokens")]
        public void Root_RoundTrip_IsDeepEqual() {
            string original_json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(original_json, _settings);
            string reserialized_json = JsonConvert.SerializeObject(root, _settings);
            JToken json_before = JToken.Parse(original_json);
            JToken json_after = JToken.Parse(reserialized_json);
            Assert.That(JToken.DeepEquals(json_before, json_after), Is.True,
                $"Round-trip mismatch.\nBefore:\n{original_json}\nAfter:\n{reserialized_json}");
        }

        [Test, Description("layout_id is preserved through round-trip")]
        public void Root_LayoutId_PreservedInRoundTrip() {
            string original_json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(original_json, _settings);
            string reserialized_json = JsonConvert.SerializeObject(root, _settings);
            Root? root2 = JsonConvert.DeserializeObject<Root>(reserialized_json, _settings);
            Assert.That(root2!.layout_id, Is.EqualTo("test_minimal"));
        }

        [Test, Description("zone_id is preserved through round-trip")]
        public void Zone_ZoneId_PreservedInRoundTrip() {
            string original_json = File.ReadAllText(Path.Combine("Fixtures", "sample_level_minimal.json"));
            Root? root = JsonConvert.DeserializeObject<Root>(original_json, _settings);
            string reserialized_json = JsonConvert.SerializeObject(root, _settings);
            Root? root2 = JsonConvert.DeserializeObject<Root>(reserialized_json, _settings);
            Assert.That(root2!.platforms[0].zones[0].zone_id, Is.EqualTo("vol_spawn"));
        }
    }
}
