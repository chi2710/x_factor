using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Nạp dữ liệu Nhiệm vụ / Vật phẩm / Trang bị / Kĩ năng từ Google Sheet (bản CSV lưu tại CsvPath),
// tạo hoặc cập nhật ScriptableObject trong Assets/Resources/Data, rồi gán Nhiệm vụ + Vật phẩm vào tab Vật phẩm.
// Cấu trúc sheet: dòng tiêu đề nhóm có tên ở cột B; nhiệm vụ có tên ở cột A, các mục khác ở cột B; mô tả ở cột C.
public static class MenuUIDataImporter
{
    private const string SheetCsvUrl = "https://docs.google.com/spreadsheets/d/1gPL2Jw5rvSOayCz85FHhEWNbPgjtEYn6orbdoPYUvuQ/export?format=csv";
    private const string DataFolder = "Assets/Resources/Data";
    private const string CsvPath = DataFolder + "/ItemData.csv";

    // Sheet không có cột khu vực cho vật phẩm; đây là khu vực suy ra từ mô tả, sửa trực tiếp trong asset nếu cần.
    private static readonly Dictionary<string, string> ItemLocations = new Dictionary<string, string>
    {
        { "PIN TĂNG ÁP", "KHÁM PHÁ KHU PHẾ THẢI" },
        { "PIN PHẾ LIỆU", "KHÁM PHÁ KHU PHẾ THẢI" },
        { "BÌNH GA THU HỒI", "TÁI CHẾ VẬT LIỆU CŨ" },
    };

    // Vật phẩm vào lưới "Vật phẩm nâng cấp" thay vì "Vật phẩm nhiệm vụ" (theo mockup).
    private static readonly HashSet<string> UpgradeItems = new HashSet<string> { "URANIUM TINH CHẾ", "LÕI NGUỒN" };

    // Dữ liệu mẫu do bản Setup trước tạo ra, nay được thay bằng dữ liệu từ sheet.
    private static readonly string[] LegacySampleAssets =
    {
        DataFolder + "/Items/Pin.asset",
        DataFolder + "/Items/NuocTangLuc.asset",
        DataFolder + "/Items/BinhGas.asset",
        DataFolder + "/Missions/TÁI CHẾ VẬT LIỆU CŨ.asset",
    };

    private static readonly HashSet<string> usedPaths = new HashSet<string>();

    private class Entry
    {
        public string section;
        public string subsection;
        public string title;
        public string description;
    }

    [MenuItem("Tools/Menu UI/Download Sheet && Import")]
    public static void DownloadAndImport()
    {
        using (var request = UnityWebRequest.Get(SheetCsvUrl))
        {
            var op = request.SendWebRequest();
            while (!op.isDone)
                EditorUtility.DisplayProgressBar("Menu UI", "Đang tải Google Sheet...", request.downloadProgress);
            EditorUtility.ClearProgressBar();

            if (request.result != UnityWebRequest.Result.Success)
            {
                EditorUtility.DisplayDialog("Menu UI", "Không tải được sheet: " + request.error, "OK");
                return;
            }
            EnsureFolder(DataFolder);
            File.WriteAllText(CsvPath, request.downloadHandler.text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CsvPath);
        }
        ImportAndAssign();
    }

    [MenuItem("Tools/Menu UI/Import Data From CSV")]
    public static void ImportAndAssign()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogWarning($"[Menu UI] Không có {CsvPath}. Dùng Tools > Menu UI > Download Sheet & Import.");
            return;
        }

        foreach (var path in LegacySampleAssets)
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.MoveAssetToTrash(path);

        usedPaths.Clear();
        var entries = Parse(File.ReadAllText(CsvPath, Encoding.UTF8));
        var missions = new List<Mission>();
        var items = new List<Item>();
        int equipment = 0, skills = 0;

        foreach (var e in entries)
        {
            switch (e.section)
            {
                case "NHIỆM VỤ":
                    missions.Add(CreateOrUpdate<Mission>("Missions", e.title, e.description, "NHIỆM VỤ"));
                    break;
                case "VẬT PHẨM":
                    ItemLocations.TryGetValue(e.title, out var location);
                    items.Add(CreateOrUpdate<Item>("Items", e.title, e.description, location));
                    break;
                case "TRANG BỊ":
                    CreateOrUpdate<Item>("Equipment", e.title, e.description, e.subsection);
                    equipment++;
                    break;
                case "KĨ NĂNG":
                    FillSkill(CreateOrUpdate<Skill>("Skills", e.title, e.description, "KĨ NĂNG"), e.description);
                    skills++;
                    break;
            }
        }
        AssetDatabase.SaveAssets();

        bool assigned = AssignToScene(missions, items);
        Debug.Log($"[Menu UI] Đã nạp {missions.Count} nhiệm vụ, {items.Count} vật phẩm, {equipment} trang bị, {skills} kĩ năng."
                  + (assigned ? " Đã gán vào tab Vật phẩm." : " Chưa gán vào scene (không tìm thấy ItemTabUI — chạy Setup Scene trước)."));
    }

    // Các asset Skill trong Data/Skills, xếp theo thứ tự trong sheet (kĩ năng không có trong sheet xếp cuối).
    public static List<Skill> SkillsInSheetOrder() => AssetsInSheetOrder<Skill>("KĨ NĂNG", "Skills");

    // Các asset kiểu T trong Data/<folder>, xếp theo thứ tự của nhóm <section> trong sheet (không có trong sheet xếp cuối).
    public static List<T> AssetsInSheetOrder<T>(string section, string folder) where T : Item
    {
        var titles = File.Exists(CsvPath)
            ? Parse(File.ReadAllText(CsvPath, Encoding.UTF8)).Where(e => e.section == section).Select(e => e.title).ToList()
            : new List<string>();
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { $"{DataFolder}/{folder}" })
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null && s.GetType() == typeof(T))
            .OrderBy(s => { int i = titles.IndexOf(s.title); return i < 0 ? int.MaxValue : i; })
            .ToList();
    }

    // ---------- Gán vào scene ----------

    private static bool AssignToScene(List<Mission> missions, List<Item> items)
    {
        var tab = Object.FindAnyObjectByType<ItemTabUI>(FindObjectsInactive.Include);
        if (tab == null)
            return false;

        // Nhiệm vụ: gán theo thứ tự, ẩn các dòng thừa.
        var missionRows = tab.GetComponentsInChildren<MissionUI>(true);
        for (int i = 0; i < missionRows.Length; i++)
        {
            var row = missionRows[i];
            Undo.RecordObject(row, "Import Menu UI Data");
            Undo.RecordObject(row.gameObject, "Import Menu UI Data");
            row.mission = i < missions.Count ? missions[i] : null;
            row.gameObject.SetActive(row.mission != null);
            if (row.mission != null)
                foreach (var text in row.titleTexts)
                {
                    Undo.RecordObject(text, "Import Menu UI Data");
                    text.text = row.mission.title;
                }
            EditorUtility.SetDirty(row);
        }

        // Vật phẩm: vào lưới "Vật phẩm nhiệm vụ" hoặc "Vật phẩm nâng cấp", các ô có vật phẩm luôn được mở khóa.
        var questItems = items.Where(i => !UpgradeItems.Contains(i.title)).ToList();
        var upgradeItems = items.Where(i => UpgradeItems.Contains(i.title)).ToList();
        foreach (var scroll in tab.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scroll.content.GetComponentInChildren<MissionUI>(true) != null)
                continue;
            string title = (scroll.transform.parent.GetComponentInChildren<TextMeshProUGUI>(true)?.text ?? "").ToUpperInvariant();
            // Lưới nâng cấp không có chữ tiêu đề (tiêu đề nằm trong ảnh), nên lưới không phải "nhiệm vụ" là lưới nâng cấp.
            var group = title.Contains("NHI") ? questItems : upgradeItems;

            var slots = scroll.content.GetComponentsInChildren<ItemUI>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                Undo.RecordObject(slots[i], "Import Menu UI Data");
                slots[i].item = i < group.Count ? group[i] : null;
                if (slots[i].item != null)
                    slots[i].isLocked = false;
                EditorUtility.SetDirty(slots[i]);
            }
        }

        EditorSceneManager.MarkSceneDirty(tab.gameObject.scene);
        return true;
    }

    // ---------- Asset ----------

    private static T CreateOrUpdate<T>(string folder, string title, string description, string location) where T : Item
    {
        string dir = $"{DataFolder}/{folder}";
        EnsureFolder(dir);
        // Tên không dấu có thể trùng (vd. "ĐAO TÁI CHẾ" và "DAO TÁI CHẾ"): thêm hậu tố theo thứ tự trong sheet.
        string slug = Slug(title), path = $"{dir}/{slug}.asset";
        for (int n = 2; !usedPaths.Add(path); n++)
            path = $"{dir}/{slug}_{n}.asset";

        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            // Asset cũ khác kiểu (vd. kĩ năng từng được nạp dưới dạng Item): giữ icon/khu vực, thay bằng asset kiểu mới.
            var old = AssetDatabase.LoadAssetAtPath<Item>(path);
            asset = ScriptableObject.CreateInstance<T>();
            if (old != null)
            {
                asset.icon = old.icon;
                asset.iconSelected = old.iconSelected;
                asset.location = old.location;
                AssetDatabase.MoveAssetToTrash(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }
        // Giữ nguyên icon và khu vực đã chỉnh tay; chỉ cập nhật tên và mô tả theo sheet.
        asset.title = title;
        asset.description = description;
        if (string.IsNullOrEmpty(asset.location))
            asset.location = location ?? "";
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void FillSkill(Skill skill, string text)
    {
        var f = ParseSkillFields(text);
        string Get(string key) => f.TryGetValue(key, out var v) ? v : "";
        skill.type = Get("type");
        skill.mech = Get("mech");
        skill.description = Get("description");
        skill.activation = Get("activation");
        skill.cost = Get("cost");
        skill.cooldown = Get("cooldown");
        skill.application = Get("application");
        skill.notes = Get("notes");
        EditorUtility.SetDirty(skill);
    }

    // Nhãn trong ô mô tả kĩ năng -> trường của Skill.
    private static readonly Dictionary<string, string> SkillLabels = new Dictionary<string, string>
    {
        { "loại", "type" },
        { "cơ chế", "mech" },
        { "mô tả", "description" },
        { "cách kích hoạt", "activation" },
        { "tiêu hao năng lượng", "cost" },
        { "tiêu hao", "cost" },
        { "thời gian hồi", "cooldown" },
        { "ứng dụng", "application" },
    };

    // Tách "Nhãn: nội dung" theo từng dòng; dòng không có nhãn nối vào trường trước đó.
    // Nhãn lạ (vd. "Giới hạn") được giữ nguyên trong "notes" để không mất dữ liệu.
    private static Dictionary<string, string> ParseSkillFields(string text)
    {
        var fields = new Dictionary<string, string>();
        string current = null;

        void Append(string key, string value)
        {
            if (value == "")
                return;
            fields[key] = fields.TryGetValue(key, out var old) && old != "" ? old + "\n" + value : value;
        }

        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            string line = raw.Trim();
            if (line == "")
                continue;

            int colon = line.IndexOf(':');
            if (colon > 0 && colon <= 25)
            {
                string label = line.Substring(0, colon).Trim();
                string value = Capitalize(line.Substring(colon + 1).Trim());
                if (SkillLabels.TryGetValue(label.ToLowerInvariant(), out var key))
                {
                    current = key;
                    Append(key, value);
                    continue;
                }
                if (!label.Contains('.'))
                {
                    current = "notes";
                    Append("notes", label + ": " + value);
                    continue;
                }
            }
            Append(current ?? "description", line);
        }
        return fields;
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);

    // "KIẾM ĐIỆN QUANG / LV1" -> "KiemDienQuangLv1"
    private static string Slug(string title)
    {
        string plain = title.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        bool upper = true;
        foreach (char c in plain)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c) && c < 128)
            {
                sb.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                upper = false;
            }
            else
                upper = true;
        }
        return sb.Length > 0 ? sb.ToString() : "Entry";
    }

    // ---------- CSV ----------

    private static List<Entry> Parse(string csv)
    {
        var entries = new List<Entry>();
        string section = null, subsection = null;

        foreach (var row in ReadRows(csv).Skip(1)) // bỏ dòng tiêu đề "Đề mục,,Nội dung"
        {
            string a = Cell(row, 0), b = Cell(row, 1), c = Cell(row, 2);
            if (a == "" && b == "" && c == "")
                continue;

            if (a == "" && b != "" && c == "")
            {
                // Dòng tiêu đề nhóm. Trong TRANG BỊ, các dòng như "VŨ KHÍ", "THIẾT BỊ" là nhóm con.
                if (section == "TRANG BỊ" && b != "KĨ NĂNG")
                    subsection = b;
                else
                {
                    section = b;
                    subsection = null;
                }
                continue;
            }

            string title = a != "" ? a : b;
            if (title == "" || section == null)
                continue;
            entries.Add(new Entry { section = section, subsection = subsection, title = title, description = c });
        }
        return entries;
    }

    private static string Cell(List<string> row, int i) => i < row.Count ? row[i].Trim() : "";

    private static IEnumerable<List<string>> ReadRows(string csv)
    {
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char ch = csv[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { cell.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else cell.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
            else if (ch == '\n' || ch == '\r')
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear();
                yield return row;
                row = new List<string>();
            }
            else cell.Append(ch);
        }
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            yield return row;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
