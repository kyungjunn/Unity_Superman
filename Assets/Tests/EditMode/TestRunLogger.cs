using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GrowNa.Tests
{
    public static class TestRunLogger
    {
        [MenuItem("GrowNa/Run EditMode Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
                => Debug.Log($"[GrowNa.Tests] 시작: {testsToRun.TestCaseCount}개");

            public void RunFinished(ITestResultAdaptor result)
            {
                var failures = new StringBuilder();
                Collect(result, failures);
                string summary = $"[GrowNa.Tests] 완료 pass={result.PassCount} fail={result.FailCount} " +
                                 $"skip={result.SkipCount} inconclusive={result.InconclusiveCount} " +
                                 $"({result.Duration:0.00}s)";
                if (result.FailCount > 0) Debug.LogError($"{summary}\n{failures}");
                else Debug.Log(summary);
            }

            static void Collect(ITestResultAdaptor node, StringBuilder sink)
            {
                if (!node.HasChildren && node.TestStatus == TestStatus.Failed)
                    sink.AppendLine($"FAIL {node.FullName}: {node.Message}");
                if (node.Children == null) return;
                foreach (var child in node.Children) Collect(child, sink);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
