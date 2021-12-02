using System;
using System.Collections.Generic;
using System.Linq;
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
        
        [TestCaseSource(typeof(Patcher_Tests), nameof(GetValueTestCases))]
        [TestCaseSource(typeof(Patcher_Tests), nameof(GetArrayTestCases))]
        [TestCaseSource(typeof(Patcher_Tests), nameof(GetObjectTestCases))]
        [TestCaseSource(typeof(Patcher_Tests), nameof(GetObjectDifficultTestCases))]
        [TestCaseSource(typeof(Patcher_Tests), nameof(GetMixedTypesTestCases))]
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

        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Patcher_GetPatch_should_throw_exception_if_any_argument_contains_DeleteNode(bool oldContainsDelete, bool newContainsDelete)
        {
            var old = oldContainsDelete ? new DeleteNode() : (ISettingsNode)new ValueNode("1"); 
            var @new = newContainsDelete ? new DeleteNode() : (ISettingsNode)new ValueNode("2");

            Assert.Throws<ArgumentException>(() => Patcher.GetPatch(old, @new));
        }

        [Test]
        public void Patcher_ApplyPatch_should_throw_exception_if_oldSettings_contains_DeleteNode()
        {
            Assert.Throws<ArgumentException>(() => Patcher.ApplyPatch(new DeleteNode(), new ValueNode("1")));
        }

        private static IEnumerable<TestCaseData> GetTestCases()
            => GetValueTestCases().Concat(GetArrayTestCases()).Concat(GetObjectTestCases()).Concat(GetObjectDifficultTestCases());

        private static IEnumerable<TestCaseData> GetValueTestCases()
        {
            foreach (var (oldName, newName) in new[]{(null, null), (null, "k"), ("k", null), ("k", "k"), ("ka", "kb")})
            foreach (var (oldValue, newValue) in new[] {(null, null), (null, "v"), ("v", null), ("v", "v"), ("va", "vb")})
                yield return Case($"{oldName}:{oldValue} -> {newName}:{newValue}", new ValueNode(oldName, oldValue), new ValueNode(newName, newName));

            TestCaseData Case(string name, ValueNode oldValue, ValueNode newValue)
            {
                return new TestCaseData(oldValue, newValue)
                {
                    TestName = $"ValueNode: {name}" 
                };
            }
        }
        
        private static IEnumerable<TestCaseData> GetArrayTestCases()
        {
            foreach (var (oldName, newName) in new[]{(null, null), (null, "k"), ("k", null), ("k", "k"), ("ka", "kb")})
            foreach (var (oldValues, newValues) in new[] {("", ""), ("", "a,b"), ("a,b", ""), ("a,b", "a,b"), ("a,b", "a,i,b"), ("a,i,b", "a,b")})
            {
                ValueNode[] GetItems(string values) => values.Split(',').Select(v => new ValueNode(v)).ToArray();

                yield return Case($"{oldName}:{oldValues} -> {newName}:{newValues}", new ArrayNode(oldName, GetItems(oldValues)), new ArrayNode(newName, GetItems(newValues)));
            }

            yield return Case("With object",
                new ArrayNode(new[] {new ObjectNode(new[] {new ValueNode("K1", "V1")})}),
                new ArrayNode(new[] {new ObjectNode(new[] {new ValueNode("K1", "V1"), new ValueNode("K2", "V2")})}));

            TestCaseData Case(string name, ArrayNode oldArray, ArrayNode newArray)
            {
                return new TestCaseData(oldArray, newArray)
                {
                    TestName = $"ArrayNode: {name}" 
                };
            }
        }
        
        private static IEnumerable<TestCaseData> GetObjectTestCases()
        {
            foreach (var (oldName, newName) in new[]{(null, null), (null, "k"), ("k", null), ("k", "k"), ("ka", "kb")})
            foreach (var (oldValues, newValues) in new[]
            {
                ("", ""), ("", "a:1,b:2"), ("a:1,b:2", ""), ("a:1,b:2", "a:1,b:2"), ("a:1,b:2", "a:1,i:0,b:2"),
                ("a:1,i:0,b:2", "a:1,b:2"), ("a:1,b:2", "a:2,b:3"), ("a:1,b:2", "a:3,b:4"), ("a:1,b:2", "a:3,b:4,c:5")
            })
            {
                ValueNode[] GetItems(string values) => values.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split(':')).Select(p => new ValueNode(p[0], p[1])).ToArray();

                var name = $"{oldName}:{oldValues} -> {newName}:{newValues}";
                var oldNode = new ObjectNode(oldName, GetItems(oldValues));
                var newNode = new ObjectNode(newName, GetItems(newValues));
                
                yield return Case(name, oldNode, newNode);
                yield return Case($"in-depth: {name}", new ObjectNode(oldName, new []{oldNode.WithName("N")}), new ObjectNode(newName, new[]{newNode.WithName("N")}));
            }

            TestCaseData Case(string name, ObjectNode oldObject, ObjectNode newObject)
            {
                return new TestCaseData(oldObject, newObject)
                {
                    TestName = $"ObjectNode: {name}" 
                };
            }
        }
        
        private static IEnumerable<TestCaseData> GetObjectDifficultTestCases()
        {
            var old = Object(null, Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B2", "2")));

            yield return Case("deep change", Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B2", "3")));
            yield return Case("deep replace", Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B3", "3")));
            yield return Case("deep add", Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B2", "2"), Value("B3", "3")));
            yield return Case("deep remove", Object("A", Value("V", "v")), Object("B", Value("B1", "1")));
            yield return Case("subtree remove", Object("A", Value("V", "v")));
            yield return Case("subtree replace", Object("A", Value("V", "v")), Object("C", Value("C1", "1"), Value("C2", "2"), Value("C3", "3")));
            yield return Case("subtree replace to value", Object("A", Value("V", "v")), Value("C", "1"));
            yield return Case("subtree replace to array", Object("A", Value("V", "v")), Array("1", "2", "3"));
            
            TestCaseData Case(string name, params ISettingsNode[] nodes)
            {
                return new TestCaseData(old, Object(null, nodes))
                {
                    TestName = $"ObjectNode: {name}" 
                };
            }
            
            ValueNode Value(string name, string value) => new ValueNode(name, value);
            
            ObjectNode Object(string name, params ISettingsNode[] nested) => new ObjectNode(name, nested);
            
            ArrayNode Array(string name, params string[] nested) => new ArrayNode(name, nested.Select(n => Value(null, n)).ToArray());
        }

        private static IEnumerable<TestCaseData> GetMixedTypesTestCases()
        {
            foreach (var a in GetValueTestCases().Select(c => c.Arguments[0]))
            foreach (var b in GetObjectDifficultTestCases().Select(c => c.Arguments[1]))
            {
                yield return new TestCaseData(a, b);
                yield return new TestCaseData(b, a);
            }
        }
    }
}