using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Tests.Helpers;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Tests.Patching
{
    public class PatchingCases
    {
        public static IEnumerable<TestCaseData> GetTestCases(string testClassName, bool rootNameNull, bool valueNotNull) =>
            GetValueTestCases(rootNameNull, valueNotNull)
                .Concat(GetArrayTestCases(rootNameNull))
                .Concat(GetObjectTestCases(rootNameNull))
                .Concat(GetObjectDifficultTestCases())
                .Concat(GetMixedTypesTestCases(rootNameNull, valueNotNull))
                .Select(c => new TestCaseData(c.Arguments) {TestName = $"{testClassName}: {c.TestName}"});

        protected static IEnumerable<TestCaseData> GetValueTestCases(bool rootNameNull, bool valueNotNull)
        {
            foreach (var (oldName, newName) in new[]{(null, null), (null, "k"), ("k", null), ("k", "k"), ("ka", "kb")})
            foreach (var (oldValue, newValue) in new[] {(null, null), (null, "v"), ("v", null), ("v", ""), ("", "v"), ("v", "v"), ("va", "vb")})
                if (!rootNameNull || oldName == null && newName == null)
                    if (!valueNotNull || oldValue != null && newValue != null)
                        yield return Case($"{oldName}:{oldValue} -> {newName}:{newValue}", new ValueNode(oldName, oldValue), new ValueNode(newName, newValue));

            TestCaseData Case(string name, ValueNode oldValue, ValueNode newValue)
            {
                return new TestCaseData(oldValue, newValue)
                {
                    TestName = $"ValueNode: {name}" 
                };
            }
        }
        
        protected static IEnumerable<TestCaseData> GetArrayTestCases(bool rootNameNull)
        {
            foreach (var (oldName, newName) in new[]{(null, null), (null, "k"), ("k", null), ("k", "k"), ("ka", "kb")})
            foreach (var (oldValues, newValues) in new[] {("", ""), ("", "a,b"), ("a,b", ""), ("a,b", "a,b"), ("a,b", "a,i,b"), ("a,i,b", "a,b")})
            {
                ValueNode[] GetItems(string values) => values.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select((v, i) => new ValueNode(i.ToString(), v)).ToArray();

                if (!rootNameNull || oldName == null && newName == null)
                    yield return Case($"{oldName}:{oldValues} -> {newName}:{newValues}", new ArrayNode(oldName, GetItems(oldValues)), new ArrayNode(newName, GetItems(newValues)));
            }

            yield return Case("With object",
                new ArrayNode(new[] {new ObjectNode("0", new[] {new ValueNode("K1", "V1")})}),
                new ArrayNode(new[] {new ObjectNode("0", new[] {new ValueNode("K1", "V1"), new ValueNode("K2", "V2")})}));

            TestCaseData Case(string name, ArrayNode oldArray, ArrayNode newArray)
            {
                return new TestCaseData(oldArray, newArray)
                {
                    TestName = $"ArrayNode: {name}" 
                };
            }
        }
        
        protected static IEnumerable<TestCaseData> GetObjectTestCases(bool rootNameNull)
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
                
                if (!rootNameNull || oldName == null && newName == null)
                {
                    yield return Case(name, oldNode, newNode);
                    yield return Case($"in-depth: {name}", new ObjectNode(oldName, new []{oldNode.WithName("N")}), new ObjectNode(newName, new[]{newNode.WithName("N")}));
                }
            }

            TestCaseData Case(string name, ObjectNode oldObject, ObjectNode newObject)
            {
                return new TestCaseData(oldObject, newObject)
                {
                    TestName = $"ObjectNode: {name}" 
                };
            }
        }
        
        protected static IEnumerable<TestCaseData> GetObjectDifficultTestCases()
        {
            var old = Object(null, Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B2", "2")));

            yield return Case("deep change", Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B2", "3")));
            yield return Case("deep replace", Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B3", "3")));
            yield return Case("deep add", Object("A", Value("V", "v")), Object("B", Value("B1", "1"), Value("B2", "2"), Value("B3", "3")));
            yield return Case("deep remove", Object("A", Value("V", "v")), Object("B", Value("B1", "1")));
            yield return Case("subtree remove", Object("A", Value("V", "v")));
            yield return Case("subtree replace", Object("A", Value("V", "v")), Object("B", Value("C1", "1"), Value("C2", "2"), Value("C3", "3")));
            yield return Case("subtree replace to value", Object("A", Value("V", "v")), Value("B", "1"));
            yield return Case("subtree replace to array", Object("A", Value("V", "v")), Array("B", "1", "2", "3"));
            yield return Case("subtree replace with new name", Object("A", Value("V", "v")), Object("C", Value("C1", "1"), Value("C2", "2"), Value("C3", "3")));
            yield return Case("subtree replace with new name to value", Object("A", Value("V", "v")), Value("C", "1"));
            yield return Case("subtree replace with new name to array", Object("A", Value("V", "v")), Array("C", "1", "2", "3"));
            
            TestCaseData Case(string name, params ISettingsNode[] nodes)
            {
                return new TestCaseData(old, Object(null, nodes))
                {
                    TestName = $"ObjectNode: {name}" 
                };
            }
            
            ValueNode Value(string name, string value) => new ValueNode(name, value);
            
            ObjectNode Object(string name, params ISettingsNode[] nested) => new ObjectNode(name, nested);
            
            ArrayNode Array(string name, params string[] nested) => new ArrayNode(name, nested.Select((n, i) => Value(i.ToString(), n)).ToArray());
        }

        protected static IEnumerable<TestCaseData> GetMixedTypesTestCases(bool rootNameNull, bool valueNotNull)
        {
            foreach (var a in GetValueTestCases(rootNameNull, valueNotNull).Select(c => c.Arguments[0]))
            foreach (var b in GetObjectDifficultTestCases().Select(c => c.Arguments[1]))
            {
                yield return new TestCaseData(a, b)
                {
                    TestName = $"MixedTypes: {a} -> {b}"
                };
                yield return new TestCaseData(b, a)
                {
                    TestName = $"MixedTypes: {b} -> {a}"
                };
            }
        }
    }
}