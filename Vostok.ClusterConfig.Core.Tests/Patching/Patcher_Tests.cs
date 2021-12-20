using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.ClusterConfig.Core.Tests.Helpers;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Tests.Patching
{
    [TestFixture]
    public class Patcher_Tests
    {
        private static readonly Patcher Patcher = new Patcher();
        
        private static IEnumerable<TestCaseData> GetCases() => PatchingCases.GetTestCases(nameof(Patcher), false, false);

        [TestCaseSource(typeof(Patcher_Tests), nameof(GetCases))]
        public void Patcher_should_return_null_when_no_diff(ISettingsNode settings, ISettingsNode _)
        {
            Patcher.GetPatch(settings, settings).Should().BeNull();
        }
        
        [TestCaseSource(typeof(Patcher_Tests), nameof(GetCases))]
        public void Patcher_should_work_correctly(ISettingsNode oldSettings, ISettingsNode newSettings)
        {
            var patch = Patcher.GetPatch(oldSettings, newSettings);
            var patchedSettings = Patcher.ApplyPatch(oldSettings, patch);

            TestContext.Out.WriteLine($"OLD SETTINGS:\n{oldSettings}\n");
            TestContext.Out.WriteLine($"NEW SETTINGS:\n{newSettings}\n");
            TestContext.Out.WriteLine($"PATCHED SETTINGS:\n{patchedSettings}\n");
            TestContext.Out.WriteLine($"PATCH:\n{patch}\n");

            if (oldSettings.Equals(newSettings))
                patch.Should().BeNull();
            else
                patch.Should().NotBeNull();

            patchedSettings.EnumerateNodes().Should().NotContain(n => n is DeleteNode);

            patchedSettings.Should().NotBeNull();
            // ReSharper disable once PossibleNullReferenceException
            patchedSettings.Equals(newSettings).Should().BeTrue();
        }
    }
}