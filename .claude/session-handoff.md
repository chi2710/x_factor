# Session handoff — Menu UI (Unity 6000.5, Sample_2D)

## Mục tiêu hiện tại

Hệ thống menu UI 4 tab (Bản đồ / Vật phẩm / Trang bị / Kĩ năng) cho game 2D, điều khiển được bằng chuột, bàn phím và tay cầm:

- Đổi tab bằng click hoặc LB/RB (Q/E).
- Menu nút Start (Tab) bật/tắt menu, B (Esc) thoát menu.
- Khóa điều khiển nhân vật khi menu đang mở.

Cả 4 tab đã được dựng. Việc gần nhất:

- Sửa lỗi menu không chọn được ô khi UI bị tắt lúc vào scene.
- Trả lời câu hỏi về việc bật menu bằng nút Menu của tay cầm. Câu trả lời: đã có sẵn, không đổi code.

## File đã thay đổi

Tất cả nằm trong `Assets/Resources/`.

**Runtime — `Scripts/UI/`**

| File | Vai trò |
|---|---|
| `SlotUI.cs` | Base cho mọi ô chọn được. Tự thêm `Selectable` trong `Awake`. Có event `Selected` (OnSelect), event `Submitted` (nút A / Enter / click trái), `SetNavigable` và tự cuộn vào khung nhìn trong `ScrollRect`. |
| `ITabContent.cs` | Interface cho nội dung tab: `GetFocusTarget()` và `HandleBack()`. |
| `TabMenuUI.cs` | Tab, LB/RB, đóng/mở menu. Event `OpenChanged`. Đồng bộ `IsOpen` khi `menuRoot` bị bật/tắt từ ngoài. Tự trả focus khi EventSystem mất ô đang chọn. |
| `TabButtonUI.cs` | Nút tab. Navigation đặt `None`. |
| `ItemTabUI.cs` | Tab Vật phẩm. Nhiệm vụ và vật phẩm chọn độc lập. Mô tả nhiệm vụ hiện ở text `MissionDetail`, tự tìm theo tên. |
| `ItemUI.cs`, `MissionUI.cs` | Ô vật phẩm và ô nhiệm vụ. User đã tự sửa `MissionUI` (thêm `icon`, `icon1`, `SetNativeSize`). |
| `ItemDetailUI.cs` | Ô Thông tin vật phẩm. Có nhánh riêng khi item là `Skill`. |
| `SkillUI.cs`, `SkillTabUI.cs`, `SkillDetailUI.cs` | Tab Kĩ năng. User đã sửa format màu trong `SkillDetailUI`. |
| `MapMarkerUI.cs`, `MapTabUI.cs` | Tab Bản đồ. |
| `EquipmentSlotUI.cs`, `EquipmentTabUI.cs` | Tab Trang bị. |

**Runtime — `Scripts/`**

- `Mission.cs`: kế thừa `Item`, thêm `completed`.
- `Skill.cs`: kế thừa `Item`. Có `type`, `mech`, `activation`, `cost`, `cooldown`, `application`, `notes`.
- `Item.cs`: user tự thêm `iconSelected`.
- `Player.cs`: `SetInputLocked`. Tắt action map Player ở cả `inputActions` lẫn `InputSystem.actions`, vì `Move` đọc từ project-wide actions.

**Editor — `Scripts/Editor/`**

| File | Menu |
|---|---|
| `MenuUISetup.cs` | **Tools > Menu UI > Setup Scene**. Gắn script vào hierarchy `UI` có sẵn và tạo panel tạm cho các tab. Lưu ý: ghi đè `MissionUI.normal`/`titleTexts`, nên tránh chạy lại sau khi user đã chỉnh tay. |
| `MenuUIDataImporter.cs` | **Download Sheet & Import** và **Import Data From CSV**. Có helper `AssetsInSheetOrder<T>` / `SkillsInSheetOrder`. |
| `SkillTabSetup.cs` | **Setup Skill Tab**. |
| `MapTabBuilder.cs` | **Build Map Tab**. Dựng lại `MapTab/MapView`. |
| `EquipmentTabBuilder.cs` | **Build Equipment Tab**. Dựng lại `EquipmentTab/EquipView`. |

**Dữ liệu — `Data/`**

- `ItemData.csv`: bản export của Google Sheet `1gPL2Jw5rvSOayCz85FHhEWNbPgjtEYn6orbdoPYUvuQ`.
- `Items/`: 4 asset.
- `Missions/`: 8 asset.
- `Equipment/`: 3 vũ khí kiểu `Item`, `location = "VŨ KHÍ"`.
- `Skills/`: 8 asset kiểu `Skill`.

## Quyết định đã chốt

- **Layout trong scene:**
  - Tab Vật phẩm và Kĩ năng do user tự dựng trong scene.
  - Tab Bản đồ và Trang bị được dựng bằng editor builder từ sprite trong `UI/1.BẢN ĐỒ` và `UI/3. TRANG BỊ`.
  - Toạ độ theo canvas tham chiếu **3840×2160**, quy đổi từ ảnh mẫu 2000px: `X = (sx-1000)*1.92`, `Y = -(sy-562.5)*1.92 + 116.15`.
- **Chọn ô:**
  - Ô trống không khóa vẫn chọn được, khi đó ô thông tin để trống. Ô khóa thì không chọn được.
  - Ở tab Vật phẩm và Trang bị, hai nhóm ô được chọn độc lập.
- **Tab Kĩ năng:**
  - A ở ô Đang sử dụng: tháo kĩ năng xuống kho rồi chuyển sang chế độ chọn.
  - A ở kĩ năng trong kho:
    - Nếu đang ở chế độ chọn: lắp vào ô vừa tháo.
    - Nếu không: lắp vào ô trống đầu tiên.
  - B: hủy chế độ chọn, không đóng menu.
  - Chặn lắp trùng kĩ năng.
  - User đã comment dòng gán skill trong `RenderStorage`, nên kho hiển thị cố định mọi kĩ năng. Giữ nguyên quyết định này.
- **Tab Trang bị:**
  - A ở thiết bị: lắp vào ô "Đang trang bị" trống đầu tiên, không trùng.
  - A ở ô "Đang trang bị": tháo ra.
  - Phần này do Claude tự thiết kế vì ảnh mẫu không nói rõ.
- **Màu nhãn:** `#19FEFE`, theo cách user tô màu trong `SkillDetailUI`.
- **Đóng/mở menu:** muốn vào game với menu đóng thì giữ object `UI` bật và bỏ chọn `startOpen`, hoặc tắt `Content`. Không tắt object `UI`, vì `TabMenuUI` cần chạy để nhận nút Menu.

## Test đang lỗi

Project không có test tự động. Kiểm tra bằng cách biên dịch toàn bộ `Assets/Resources/Scripts` với Roslyn có sẵn trong Unity. Lần biên dịch cuối: **0 lỗi**.

- Compiler: `D:\Unity\6000.5.0f1\Editor\Data\DotNetSdk\dotnet.exe` chạy `...\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll`.
- Danh sách reference lấy từ HintPath trong `Assembly-CSharp*.csproj`, đã loại trùng theo tên file.
- Phải chạy từ `D:\Sample_2D`, vì đường dẫn reference là tương đối.

Các thay đổi gần nhất **đã được user xác nhận chạy đúng trong Play mode (2026-11-03)**:

- Đồng bộ `IsOpen` khi UI bật lại.
- Tự trả focus về tab.
- `GetFocusTarget` an toàn khi gặp null.

## Nguyên nhân gốc (lỗi gần nhất)

Khi `Content` (`menuRoot`) bị tắt lúc vào scene, `TabMenuUI.Start` gọi `SelectTab` rồi `FocusTabContent`. Lúc đó tab chưa hiển thị, nên `Awake` của nó chưa chạy và mảng ô vẫn là null. `ItemTabUI.GetFocusTarget` duyệt mảng null và ném `NullReferenceException`.

Hệ quả:

- `Start` dừng trước `Open()`, nên `IsOpen` kẹt ở `false`.
- Khi bật UI lại, không có ô nào được chọn, LB/RB và B bị bỏ qua.

Lỗi "viền đỏ không di chuyển" ở tab Trang bị trước đó nhiều khả năng cùng nguyên nhân.

Cách sửa:

- `Update` đồng bộ `IsOpen` với `menuRoot.activeInHierarchy`.
- `FocusTabContent` bỏ qua tab chưa hiển thị.
- Cả 4 `GetFocusTarget` trả về `null` nếu chưa `Awake`.
- `Start` giữ trạng thái đóng nếu `menuRoot` đang tắt sẵn.

## Bước tiếp theo

1. ~~Xác nhận Play mode~~: xong, user báo chạy đúng.
   - Lưu ý biên dịch: phải thêm `Assets/InputSystem_Actions.cs` vào nguồn, và bỏ 3 shim `netstandard/System.Reflection.Emit*` bị trùng.
2. Nếu user muốn nút Menu chạy được cả khi object `UI` bị tắt hẳn: tách phần nhận phím toggle ra một component riêng trên object luôn bật.
3. Việc còn lại:
   - Bản đồ: 3 marker "!" đỏ chưa có sprite. Nút Y "Dịch chuyển" chưa có chức năng.
   - Trang bị: nhóm THIẾT BỊ trong sheet đang trống. Thiếu ảnh vũ khí. Nút "NÂNG CẤP: 20Đ" chỉ là hình.
   - Thanh gợi ý nút (A/B/Điều hướng) chưa được dựng.
4. Lưu ý về Skill.cs: Claude từng ghi đè file `Skill.cs` do user tạo mà không đọc trước, và không khôi phục được bản gốc. Luôn đọc file trước khi ghi. User thường tự sửa file giữa các lượt, nên phải đọc lại trước khi sửa.
