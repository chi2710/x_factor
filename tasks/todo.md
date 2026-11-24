# Todo

## Player không nhận input (2026-11-05)

- [x] Đọc logic khóa input: `Player.SetInputLocked` theo `TabMenuUI.IsOpen`
- [x] Kiểm tra scene: `TabMenuUI.menuRoot` chưa gán, con đầu tiên của `UI` là `HUD`
- [x] Sửa `TabMenuUI.Awake`: tìm `menuRoot` theo tên `Content` thay vì con đầu tiên
- [x] Biên dịch toàn bộ scripts: 0 lỗi
- [ ] User xác nhận trong Play mode

### Review

- **Nguyên nhân:** `menuRoot` để trống nên `Awake` lấy con đầu tiên của `UI`. Sau khi thêm `HUD`, con đầu tiên là `HUD` chứ không còn là `Content`. `HUD` luôn hiện, nên `Update` coi menu luôn mở (`IsOpen = true`) và `Player` bị khóa action map Player.
- **Cách sửa:** fallback theo tên `Content` và cảnh báo nếu không tìm thấy. Không phụ thuộc thứ tự con trong hierarchy nữa.

## Settings UI (2026-11-07)

Panel toàn màn hình `UI/Settings`, tách riêng khỏi `Content` của TabMenu. Có 5 tab: Hiển thị, Đồ họa, Âm thanh, Điều khiển, Ngôn ngữ.

- **Âm thanh** có logic đầy đủ.
- **Điều khiển** chỉ hiện ảnh tay cầm.
- **Ba tab còn lại** để trống.

### Script runtime (`Scripts/UI/Settings/`)

- [x] **`GameSettings`** (static): giữ 4 giá trị, kèm event `Changed`.
  - Các giá trị: `MasterVolume` (0–100, mặc định 60), `Music` (bool, mặc định BẬT), `Sfx` (bool, mặc định TẮT), `AmbientVolume` (0–100, mặc định 100).
  - `Load`/`Save` qua PlayerPrefs.
  - `Apply`: gán `AudioListener.volume = Master/100`. Project chưa có AudioMixer, nên Music/Sfx/Ambient chỉ được lưu và báo qua event để hệ thống âm thanh sau này đọc.
- [x] **`SettingRowUI`** (kế thừa `Selectable`): một dòng gồm nhãn và giá trị.
  - Lên/xuống: điều hướng như bình thường.
  - Trái/phải: đổi giá trị. Slider mỗi lần ±5. Dòng bật/tắt đổi giữa BẬT và TẮT.
  - Dòng đang chọn: bật nền đỏ (`Asset 183`), mũi tên đổi sang trắng (`189`). Dòng không chọn dùng mũi tên cyan (`188`).
  - Slider: vạch đỏ (`185`) và số chạy dọc theo thanh.
- [x] ~~**`SettingsTabUI`**~~ dùng lại `TabButtonUI` (thêm `selectedLabelOffset`): nút tab bên trái.
  - Tab đang chọn: khung `173`, mũi tên `174`, chữ có Glow (dùng lại preset của `TabGlowSetup`).
- [x] **`SettingsUI`**:
  - LB/RB (Q/E) đổi tab, B (Esc) đóng.
  - **Áp dụng** gọi `Save` + `Apply`. **Đặt về mặc định** reset giá trị trên giao diện.
  - Đóng mà chưa bấm Áp dụng thì bỏ thay đổi.
  - Từ dòng cuối bấm xuống sẽ tới hai nút. Event `OpenChanged` để khóa Player.

### Editor (`Scripts/Editor/SettingsBuilder.cs`)

- [x] **Tools > Menu UI > Build Settings**: dựng `UI/Settings` từ sprite trong `UI/SETTING`.
  - Sprite thiết kế cho 1920, nên trên canvas 3840 nhân ×2. Toạ độ = toạ độ ảnh mẫu (2000px) × 1.92.
  - Nền `175`, `190`. Header `176` (đã có chữ tiêu đề). Viền dưới `180`. Khung tab `172`.
  - Nút `179` (Áp dụng) và `178` (Đặt về mặc định), thanh gợi ý nút `181`, ảnh tay cầm `182`, `>>>` `184`.
  - Chữ dùng font CascadiaCode (Bold cho tab và tiêu đề, Regular cho các dòng).

### Cần user quyết

- [x] Mở Settings: nút Start/Menu (Gamepad start) + phím O. Menu Tab đổi sang Select/View (2026-11-10)
- [x] Khóa input: có, `Player.RefreshInputLock` đọc cả TabMenuUI và SettingsUI

### Kiểm tra

- [x] Biên dịch toàn bộ scripts bằng Roslyn: 0 lỗi
- [ ] User xác nhận trong Play mode

### Review

- **Script runtime:** `Scripts/UI/Settings/`: `GameSettings`, `SettingRowUI`, `SettingsUI`.
- **Điều hướng:** nối lúc chạy, theo số dòng của tab đang mở.
  - Các dòng xếp dọc. Dòng cuối bấm xuống tới Áp dụng, Áp dụng bấm phải tới Đặt về mặc định.
  - Tab không có dòng nào thì focus vào Áp dụng.
- **Settings và menu Tab:** không mở cùng lúc. Mở bên này sẽ đóng bên kia, vì cả hai dùng chung LB/RB và B.
- **Tab:** dùng lại `TabButtonUI` và preset glow của `TabGlowSetup`. Hàm `GetOrCreateGlowPreset` được đổi thành public.
- **Chưa kiểm tra:**
  - Toạ độ ước lượng từ ảnh mẫu.
  - Vị trí `>>>` sau tiêu đề phụ thuộc độ rộng font thực tế.
  - Chưa chạy menu Build và chưa test trong Play mode.

## Nhặt item ngoài level (2026-11-20)

Pin tăng áp và Túi dịch xung đặt ngoài level. Chạm vào là nhặt, hiện popup, rồi item vào kho trong tab Vật phẩm.

User đã chốt:
- **Cách nhặt:** chạm là nhặt.
- **Popup:** tạm dùng asset có sẵn (khung ô Thông tin). Bấm A, Enter hoặc click để đóng. Game dừng trong lúc popup hiện.
- **Kho:** item chỉ hiện khi đã nhặt, điền vào ô trống đầu tiên của lưới "VẬT PHẨM NHIỆM VỤ". Các item gán sẵn khác giữ nguyên. Không lưu qua lần chơi sau.

### Script
- [x] `Inventory` (static): danh sách item đã nhặt, `Add`, `Has`, event `ItemAdded`. Reset khi vào Play.
- [x] `ItemPickup`: trigger 2D. Player chạm vào thì gọi `Inventory.Add` rồi tắt object.
- [x] `ItemPickupPopup`: nghe `ItemAdded`, hiện chi tiết item bằng `ItemDetailUI`. Dừng game (`timeScale = 0`), có hàng đợi nếu nhặt liền nhiều item. A, Enter hoặc click để đóng. Có event `OpenChanged`.
- [x] `ItemTabUI`: thêm `pickupSlots`. Mỗi lần tab bật, điền các item đã nhặt mà chưa có ô vào ô trống, chưa khóa, đầu tiên.
- [x] `Player`: khóa input khi popup mở (cùng cách với menu và Settings).

### Scene
- [x] Bỏ Pin tăng áp và Túi dịch xung khỏi ô `Item` và `Item (2)` của lưới Nhiệm vụ.
- [x] Gán `pickupSlots` = `Item/Scroll View/Viewport/Content` (lưới Nhiệm vụ).
- [x] 2 object `Pickup_PinTangAp` (x ≈ 23.4, trên ban công tòa nhà phải) và `Pickup_TuiDichXung` (x ≈ -13.3, trên gờ tòa nhà xanh) trong `Scene01`. Mỗi object có sprite iconSelected, BoxCollider2D trigger và `ItemPickup`.
- [x] `UI/ItemPopup`: nền tối, bản sao `ItemDetail` đặt giữa màn, tiêu đề "NHẬN ĐƯỢC VẬT PHẨM".

### Kiểm tra
- [x] Biên dịch toàn bộ scripts bằng Roslyn: 0 lỗi
- [x] YAML scene: ID không trùng, mọi tham chiếu đều có, cha và con khớp nhau
- [ ] User xác nhận trong Play mode

### Review

- **Script mới:** `Inventory` (static), `ItemPickup`, `UI/ItemPickupPopup`. Có sửa `ItemTabUI` (thêm `pickupSlots`, điền ô trong `OnEnable`) và `Player` (khóa input khi popup mở).
- **Vị trí item:** đo trên ảnh tham chiếu `0.khu dan cu.png` và đối chiếu collider `GROUND 2`.
  - Ban công phải: mặt sàn y ≈ -0.50, x 22.1..26.2.
  - Gờ tòa nhà xanh: mặt sàn y ≈ -0.69, x -17.4..-8.7.
  - Item dùng scale 0.15 (cao khoảng 0.77) và sorting order 6.
- **Popup:** `UI/ItemPopup` là con cuối của canvas `UI`. Panel tối (alpha 0.75) chặn click; bản sao `ItemDetail` được đặt để khung `Bg` nằm giữa màn; tiêu đề "NHẬN ĐƯỢC VẬT PHẨM".
- **Kiểm tra YAML:** 2813 doc không trùng ID, không có tham chiếu treo. Các cảnh báo cha/con còn lại đều là transform `stripped` của prefab fog có sẵn.
- **Chưa kiểm tra:**
  - Vị trí item trên màn hình thật (ảnh tham chiếu và lớp ground có thể lệch nhau một chút).
  - Popup và việc dừng game trong Play mode.
## Enemy: Animator, vật lý, chết sau 2 đòn (2026-11-22)

User đã chốt:
- **Idle:** lặp frame 0–5 của robot `Sprites/Enemy/0.png`.
- **Hurt:** tạm giống Idle, user sẽ thay sau.
- **Đánh trúng:** tính theo Animation Event ở frame chém.
- **Behavior Tree (Opsive):** tắt.

- [x] `Animation/Enemy/Enemy_Idle.anim` (0_0..0_5, 8 fps, lặp), `Enemy_Hurt.anim` (cùng frame, không lặp), `Enemy.controller` (Idle mặc định, Hurt)
- [x] `EnemyHealth`: `TakeHit()`. Trúng thì chạy Hurt rồi về Idle. Trúng đủ `maxHits` (2) thì tắt collider và vật lý, chạy Hurt xong thì tắt object.
- [x] `Player.AttackHit()` (gọi từ Animation Event): `OverlapBoxAll` trước mặt (offset theo `transform`, nên tự lật khi quay mặt). Mỗi event, mỗi enemy chỉ bị trúng 1 lần. Có Gizmo để xem vùng đánh.
- [x] Animation Event `AttackHit`:
  - Attack: 0.167 s
  - Attack_2: 0.167 s và 1.042 s
  - Attack_3: 0.167 s, 1.042 s và 1.625 s
- [x] Scene `Enemy`: thêm Animator, Rigidbody2D (khóa xoay và trục X), BoxCollider2D, `EnemyHealth`. Tắt `BehaviorTree`.
- [x] Biên dịch 0 lỗi, kiểm tra YAML
- [ ] User xác nhận trong Play mode

### Review
- **Asset mới:**
  - `Animation/Enemy/Enemy_Idle.anim` (lặp), `Enemy_Hurt.anim`, `Enemy.controller` (Idle mặc định, Hurt).
  - Script `Scripts/EnemyHealth.cs`.
- **Player:** thêm `AttackHit()`, `attackHitboxOffset` (1.3, 1.6) và `attackHitboxSize` (2.4, 2.6), cùng Gizmo đỏ khi chọn Player. Ba clip tấn công đã có event `AttackHit`.
- **Scene:**
  - `Enemy` có thêm Animator, Rigidbody2D (khóa X và xoay), BoxCollider2D 2 × 1.4, `EnemyHealth`.
  - `BehaviorTree` đã tắt.
  - Object `Enemy` vẫn đang tắt như trước (`m_IsActive: 0`).
- **Chưa kiểm tra:**
  - Thời điểm frame chém và kích thước hitbox trong Play mode.
  - Hướng mặt gốc của sprite tấn công (trong ảnh frame, kiếm vung sang trái).