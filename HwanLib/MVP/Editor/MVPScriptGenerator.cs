using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MVP.Editor
{
    /// <summary>
    /// M/P/V 스크립트 3종 템플릿 생성기. 씬/프리팹 배선은 하지 않고 파일만 만든다.
    /// </summary>
    public static class MVPScriptGenerator
    {
        public const string DefaultExcludeKeywords = "Assets,_Scripts";
        private const string ExcludePrefsKey = "MVP.ScriptGen.ExcludeKeywords";
        private const string BaseFolder = "Assets/_Scripts/UI";
        private const string UIIdPath = "Assets/HwanLib/MVP/System/GenerateUI/UIId.cs";
        private const string OpenMarker = "// <generated>";
        private const string CloseMarker = "// </generated>";

        public struct Result
        {
            public bool Success;
            public string Message;
            public string FolderPath;
        }

        // ── 네임스페이스 도출 (순수 로직) ───────────────────────────────────────────

        /// <summary>
        /// 폴더 경로를 '/'로 분할하고 제외 키워드 세그먼트를 제거한 뒤 '.'으로 결합한다.
        /// 예: "Assets/_Scripts/UI/HUD/Inventory" + {Assets,_Scripts} → "UI.HUD.Inventory"
        /// </summary>
        public static string DeriveNamespace(string folderPath, IEnumerable<string> excludeKeywords)
        {
            var exclude = new HashSet<string>(excludeKeywords ?? Enumerable.Empty<string>());
            IEnumerable<string> segments = folderPath
                .Replace('\\', '/')
                .Split('/')
                .Where(s => !string.IsNullOrEmpty(s) && !exclude.Contains(s));
            return string.Join(".", segments);
        }

        // ── 제외 키워드 영속화 ──────────────────────────────────────────────────────

        public static string LoadExcludeKeywordsRaw() =>
            EditorPrefs.GetString(ExcludePrefsKey, DefaultExcludeKeywords);

        public static void SaveExcludeKeywordsRaw(string raw) =>
            EditorPrefs.SetString(ExcludePrefsKey, raw);

        public static string[] ParseKeywords(string raw) =>
            (raw ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        // ── 생성 ────────────────────────────────────────────────────────────────────

        public static Result Generate(string category, string name, IEnumerable<string> excludeKeywords)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(name))
                return Fail("분류와 이름을 모두 입력하세요.");

            category = category.Trim();
            name = name.Trim();

            if (!IsValidIdentifier(name))
                return Fail($"이름이 유효한 C# 식별자가 아닙니다: '{name}'");

            string folder = $"{BaseFolder}/{category}/{name}";
            string ns = DeriveNamespace(folder, excludeKeywords);

            var files = new Dictionary<string, string>
            {
                [$"{name}Model.cs"] = ModelTemplate(ns, name),
                [$"{name}Presenter.cs"] = PresenterTemplate(ns, name),
                [$"{name}View.cs"] = ViewTemplate(ns, name),
            };

            string absFolder = ToAbsolute(folder);
            foreach (string fileName in files.Keys)
            {
                if (File.Exists(Path.Combine(absFolder, fileName)))
                    return Fail($"이미 존재하는 파일이 있어 생성을 중단합니다: {folder}/{fileName}");
            }

            Directory.CreateDirectory(absFolder);
            foreach (var kv in files)
                File.WriteAllText(Path.Combine(absFolder, kv.Key), kv.Value);

            string idMessage = AppendUIId(name);

            AssetDatabase.Refresh();
            return new Result
            {
                Success = true,
                FolderPath = folder,
                Message = $"생성 완료: {folder}  (네임스페이스 {ns}){idMessage}",
            };
        }

        // ── UIId enum 자동 추가 ───────────────────────────────────────────────────────

        public static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            for (int i = 1; i < name.Length; i++)
                if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_')) return false;
            return true;
        }

        /// <summary>
        /// UIId.cs의 &lt;generated&gt; 마커 영역에 idName 항목을 max+1 값으로 append.
        /// 결과 메시지(빈 문자열 = 추가 안 함)를 반환. 파일이 없거나 마커가 없으면 경고만.
        /// </summary>
        public static string AppendUIId(string idName)
        {
            string abs = ToAbsolute(UIIdPath);
            if (!File.Exists(abs))
            {
                Debug.LogWarning($"[MVPSetup] UIId.cs를 찾지 못해 UIId 추가를 건너뜁니다: {UIIdPath}");
                return "";
            }

            string src = File.ReadAllText(abs);
            string updated = AppendUIIdToSource(src, idName, out bool added);
            if (added)
            {
                File.WriteAllText(abs, updated);
                return $"\nUIId.{idName} 추가됨.";
            }
            return $"\nUIId.{idName} 이미 존재(추가 생략).";
        }

        /// <summary>
        /// 순수 문자열 변환(테스트 가능). 마커 영역 기존 항목을 보존하고 새 항목만 max+1로 추가한다.
        /// 이미 같은 이름이 있으면 원본을 그대로 반환하고 added=false.
        /// </summary>
        public static string AppendUIIdToSource(string source, string idName, out bool added)
        {
            added = false;

            int open = source.IndexOf(OpenMarker, StringComparison.Ordinal);
            int close = source.IndexOf(CloseMarker, StringComparison.Ordinal);
            if (open < 0 || close < 0 || close < open)
                throw new InvalidOperationException("UIId.cs에서 <generated> 마커를 찾지 못했습니다.");

            string region = source.Substring(open, close - open);

            int max = 0;
            foreach (Match m in Regex.Matches(region, @"(\w+)\s*=\s*(\d+)"))
            {
                if (m.Groups[1].Value == idName) return source; // 멱등: 이미 존재
                int v = int.Parse(m.Groups[2].Value);
                if (v > max) max = v;
            }

            // close 마커 라인의 들여쓰기를 재사용해 그 앞에 새 항목 줄을 삽입.
            int lineStart = source.LastIndexOf('\n', close) + 1;
            string indent = source.Substring(lineStart, close - lineStart);
            string nl = source.Contains("\r\n") ? "\r\n" : "\n";
            string newLine = $"{indent}{idName} = {max + 1},{nl}";

            added = true;
            return source.Insert(lineStart, newLine);
        }

        private static Result Fail(string message) => new() { Success = false, Message = message };

        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        // ── 템플릿 ──────────────────────────────────────────────────────────────────

        private static string ModelTemplate(string ns, string name) =>
$@"using MVP.System.BaseMVP;

namespace {ns}
{{
    public class {name}Model : IModel
    {{
    }}
}}
";

        private static string PresenterTemplate(string ns, string name) =>
$@"using MVP.System.BaseMVP;

namespace {ns}
{{
    public class {name}Presenter : BasePresenter<{name}Model, {name}View>
    {{
    }}
}}
";

        private static string ViewTemplate(string ns, string name) =>
$@"using MVP.System.BaseMVP;

namespace {ns}
{{
    public class {name}View : BaseView
    {{
    }}
}}
";
    }
}
