using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.ClusterConfig.Core.Serialization;
using Vostok.ClusterConfig.Core.Tests.Helpers;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Tests.Patching
{
    [TestFixture]
    public class TreeSerializerV2_Patching_Tests
    {
        private static readonly Patcher Patcher = new Patcher();
        private static readonly TreeSerializerV2 Serializer = new TreeSerializerV2();

        private static IEnumerable<TestCaseData> GetCases() => PatchingCases.GetTestCases(nameof(TreeSerializerV2), true, true);

        [TestCaseSource(typeof(TreeSerializerV2_Patching_Tests), nameof(GetCases))]
        public void TreeSerializerV2_should_work_correctly(ISettingsNode oldSettings, ISettingsNode newSettings)
        {
            var patch = Patcher.GetPatch(oldSettings, newSettings);
            
            var oldSettingsBin = Serialize(oldSettings);
            var newSettingsBin = Serialize(newSettings);
            var patchBin = Serialize(patch);
            
            var patchedSettingsBin = ApplyPatch(oldSettingsBin, patchBin);
            var patchedSettings = Serializer.Deserialize(patchedSettingsBin);

            TestContext.Out.WriteLine($"OLD SETTINGS:\n{oldSettings}\n");
            TestContext.Out.WriteLine($"NEW SETTINGS:\n{newSettings}\n");
            TestContext.Out.WriteLine($"PATCHED SETTINGS:\n{patchedSettings}\n");
            TestContext.Out.WriteLine($"PATCH:\n{patch}\n");

            PrintBin("OLD SETTINGS BIN", oldSettingsBin);
            PrintBin("NEW SETTINGS BIN", newSettingsBin);
            PrintBin("PATCHED SETTINGS BIN", patchedSettingsBin);
            PrintBin("PATCH SETTINGS BIN", patchBin);

            if (oldSettings.Equals(newSettings))
                patch.Should().BeNull();
            else
                patch.Should().NotBeNull();

            patchedSettings.EnumerateNodes().Should().NotContain(n => n is DeleteNode);

            patchedSettings.Should().NotBeNull();
            // ReSharper disable once PossibleNullReferenceException
            patchedSettings.Equals(newSettings).Should().BeTrue();

            patchedSettingsBin.Should().BeEquivalentTo(newSettingsBin);
        }

        [Test]
        public void TreeSerializerV2_should_supports_full_tree_deletion()
        {
            var old = new ObjectNode(null, new ISettingsNode[] {new ValueNode("a", "b"), new ArrayNode("c")});
            var patch = new DeleteNode();

            var oldBin = Serialize(old);
            var patchBin = Serialize(patch);

            var patchedBin = ApplyPatch(oldBin, patchBin);

            patchedBin.Should().BeEmpty();
            Serializer.Deserialize(patchedBin).Should().BeNull();
        }

        private byte[] Serialize(ISettingsNode tree)
        {
            var writer = new BinaryBufferWriter(1024);
            
            Serializer.Serialize(tree, writer);

            return writer.FilledSegment.ToArray();
        }

        private byte[] ApplyPatch(byte[] settings, byte[] patch)
        {
            var writer = new BinaryBufferWriter(1024);
            
            Serializer.ApplyPatch(settings, patch, writer);

            return writer.FilledSegment.ToArray();
        }

        private static void PrintBin(string name, byte[] bin)
        {
            TestContext.Out.WriteLine($"{name} ({bin.Length}):\n{string.Join("\n", bin.Select(b => $" {b:X}"))}\n");
        }
    }
}