# Nhật ký tiến độ — Sample_2D

Mỗi phiên thêm một mục mới ở **đầu** danh sách. Bối cảnh chi tiết và các quyết định đã chốt nằm trong `session-handoff.md`.

## 2026-11-17
- `BamTuong.iconSelected` đổi từ `Asset 148` (màu đỏ) sang `4.KĨ NĂNG/Asset 84` (màu cam) theo user.
- Tab Kĩ năng: kĩ năng đã lắp hiện `iconGray` trong kho và không chọn/bấm được (`SkillUI.SetEquipped`, `CanSelect` = !isLocked && !equipped), thay cho làm mờ CanvasGroup (bỏ `SetDimmed`/`equippedAlpha`). Thêm `Skill.iconGray`, gán 8 asset từ `4.KĨ NĂNG/Asset 12x/148 gray` (sprite Multiple, fileID theo internalID). Lắp từ kho thì focus nhảy sang ô kho kế tiếp còn chọn được. Biên dịch 0 lỗi, chưa test Play mode.
- Tab Trang bị + dữ liệu theo file Excel `TÁC NHÂN X — Nội dung UI UX.xlsx` (Downloads), sheet `Noi_dung` xuất đè `Data/ItemData.csv`. Asset sửa tay (không chạy Import): 3 vũ khí cập nhật mô tả có chỉ số; sửa icon ĐAO/DAO bị tráo (ĐAO → `3. TRANG BỊ/Layer 42 copy`, DAO → `Item/Group 12`). Thêm `Equipment/DayDien`, `CoLe`, `MayDoDien` (`Asset 246/245/247@2x`, location `THIẾT BỊ`) và `Items/UraniumTinhChe` (`Untitled-1`), `LoiNguon` (`thay2`), location trống. Scene: `Device_0..2` gán 3 thiết bị; lưới nâng cấp `Item (1)` ô 0–1 gán Uranium, Lõi nguồn; `WeaponName` màu cam `#FF6B2B`; thêm `WeaponLine` (`Asset 249@2x`) và `DeviceLine` (`Asset 248@2x`). `EquipmentTabUI.ColorLabels` chèn dòng trống trước khối "Nhãn:". Importer đưa Uranium/Lõi nguồn vào lưới nâng cấp; builder thêm 2 đường nối + màu cam. Biên dịch 0 lỗi, chưa xem trong Unity.
- Tab Bản đồ theo mockup (`UI UX` bản đồ): `MapMarkerUI.selectedScale` (1.4) chỉ phóng to điểm đang chọn. `MapTabBuilder`: `Marker_KeDich` thu về 103×104 (cỡ chưa chọn), thêm 3 `Marker_Alert1..3` (`Asset 151`, chưa gắn Mission) và `ButtonHints` (`Asset 152`, 918×68 tại 1219,-839). Điều hướng giữa điểm dùng Navigation Automatic có sẵn. Theo yêu cầu user, áp thẳng vào `SampleScene.unity` (không chạy menu): KeDich 103×104, 3 `Marker_Alert1..3`, `ButtonHints`, `ProgressIcon` 199×74, `ProgressText` tắt. Builder vẫn khớp với scene. Biên dịch 0 lỗi, chưa test Play mode. `Asset 91` có sẵn chữ "60%": builder ẩn `ProgressText`, `ProgressIcon` 199×74 tại (1123, 622).
- Icon tab menu theo mockup: thêm object `Icon` (Image, scale 2, native size) vào `ButtonTab (1)` Vật phẩm (`3. TRANG BỊ/Asset 62`, x -131), `(2)` Trang bị (`Asset 64`, x -120), `(3)` Kĩ năng (`Asset 63`, x -120), sửa thẳng `SampleScene.unity` theo yêu cầu user. `ButtonTab` Bản đồ đã có sẵn Icon (`png/Asset 12`). Vị trí x ước lượng từ mockup, chưa xem trong Unity.
- Thêm `TabSelection/LB` (x -1108) và `TabSelection/RB` (x 1110), y 47.85, anchor đáy giữa. TMP copy từ `Name` của tab (cùng font, size 30), màu `#19FEFE`. Con `Arrow` dùng `png/Asset 9` scale ±2. Sửa thẳng scene, chưa xem trong Unity.
- Độ dài handle scrollbar tab Vật phẩm theo mockup (`png/UI UX-01.jpg`). ScrollRect tự ghi đè `Scrollbar.size` mỗi frame nên thêm `Scripts/UI/FixedScrollbarHandle.cs` (gán lại size trong `Canvas.willRenderCanvases`). Giá trị đo từ mockup: `Mission` 0.34, `Item` 0.67, `Item (1)` 0.52. Theo yêu cầu user, Claude gắn thẳng component vào 3 `Scrollbar Vertical` trong `SampleScene.unity` (fileID 122784110, 230913934, 1418494245). Biên dịch 0 lỗi, chưa mở scene trong Unity, chưa test Play mode.

## 2026-11-15
- Viết lại `AGENTS.md` và `init.sh` cho Unity, bỏ git và npm/JS/TS.
- `init.sh` biên dịch thử `Assets/Resources/Scripts` bằng Roslyn của Unity: **0 lỗi, 2 warning**.
- Điền `feature_list.json` và file này từ `session-handoff.md`.
- Enemy AI (Behavior Designer Pro 3.3.4): thêm 3 task 2D trong `Scripts/AI/` (`Patrol2D`, `Chase2D`, `IsTargetInFront2D`) + helper `Facing2D`. Khoảng cách dùng `DistanceComparison` có sẵn. Biên dịch 0 lỗi. Tree chưa dựng, chưa test Play mode.
- Thêm `Scripts/Editor/EnemyTreeBuilder.cs` (**Tools > Enemy AI > Build Behavior Tree**) dựng cây bằng code. Dựa trên API public nhưng không có tài liệu của BD (task thường bọc trong `StackedAction`/`StackedConditional`, giống prefab mẫu). Chưa chạy trong Unity.
- 2026-11-16: Pivot các sprite con `Sprites/Player/Attack/ATK 2- 001..003` đặt Bottom Center. Ghi lại `Animation/Player/Player_Attack_2.anim` (19 frame, 12 fps, không loop, giữ guid clip vì `Player.controller` đang dùng). Bản cũ trỏ tới texture đã xóa. Chưa xem trong Unity.
- `Player_Attack_2`: 24 fps, mặc định on twos; frame 11, 16 on ones; 3, 9, 17 on threes (đếm từ 1). Dài 1.625 s.
- Pivot sprite con `ATK 3- 001..004` đặt Bottom Center. Tạo mới `Player_Attack_3.anim`: 27 frame (bỏ mảnh vụn `ATK 3- 002_5` 77×55 px theo ý user), 24 fps, on twos; 11, 13, 15, 23 on ones; 3, 9, 24, 26, 27 on threes. Dài 2.29 s. Chưa thêm vào `Player.controller`.
- Đòn đánh 2 / 3: thêm action `Attack2` (`<Gamepad>/buttonNorth`) và `Attack3` (`<Gamepad>/dpad/down`) vào map Player trong `InputSystem_Actions.inputactions`, chỉ có binding tay cầm. `Player.cs` lấy hai action này bằng `FindAction` và có `StartAttack(anim, duration)`. Thời gian khóa bằng độ dài clip. Thêm state `Player_Attack_3` vào `Player.controller`. Y vẫn còn binding `Interact` (không code nào dùng). Chưa test Play mode.
- VFX khi quay trái: `vfx_chem_02`/`03` có Start Rotation khác 0 (≈ -111°, -19°) nên chỉ lật texture là sai góc. `Player.SetFacing` giờ đảo thêm `main.flipRotation` (1 - giá trị gốc). Chưa test Play mode. `vfx_chem_02`/`03` vẫn chưa được clip nào bật.
- 2026-11-21: Đổi bind: `Attack2` = D-pad xuống, `Attack3` = Y. Combo 2/3 lướt về hướng mặt `comboLungeDistance` = 0.5 trong `comboLungeDuration` = 0.15 s lúc đầu đòn (Header "Combo Lunge" trong Inspector). Đoạn xử lý dốc giữ tốc độ theo `|vel.x|` thay vì luôn bằng `walkSpeed`. Chưa test Play mode.
- `vfx_run_dust` không thấy khi chạy: Sorting Order 0, thấp hơn Player/`GROUND1_0` (4) và bằng nền `0.khu dan cu`. Đã ghi đè `m_SortingOrder = 5` trên instance trong `player.prefab` (prefab VFX gốc giữ nguyên). Emission 2/s, lifetime 0.5 s nên hạt đầu ra sau 0.5 s: `ChangeAnimationState` giờ gọi thêm `runDust.Emit(1)` khi vào anim Move. Chưa test Play mode.
- `vfx_dash` (Phong Design/Prefabs/Prefabs_2): tắt child `Particle System` (dash_wind). `vfx_dash` gốc (dash_electric) và `dash` (vfx dash) chuyển sang trail: bật Trails module (world space), Render Mode = None, trail material mới `dash_electric_trail.mat` / `vfx_dash_trail.mat` dùng 1 frame cắt từ sheet (`dash_electric_trail.png` hàng 4, `vfx_dash_trail.png` ô hàng 1 cột 3), độ rộng trail 0.65 / 0.43 theo tỉ lệ cắt. `dash_01` (Aura_01) giữ nguyên. Backup prefab ở scratchpad. Chưa xem trong Unity.
- Tạm tắt Trails module của `vfx_dash` gốc và `dash` (Render Mode None nên không hiện gì; bật lại Trails là có trail). Aura `dash_01`: thêm burst 1 hạt tại t=0 (trước đó hạt đầu ra ở 0.1 s), looping = 1. `Player_Dash.anim`: `vfx_dash` giữ active suốt state Dash (trước tắt ở 0.29 s, trong khi `dashDuration` = 0.3 trong player.prefab). Backup `vfx_dash.prefab.bak2`, `Player_Dash.anim.bak` ở scratchpad. Chưa xem trong Unity.
- `startJump` gọi `runDust.Stop(true, StopEmittingAndClear)`: nhảy là xóa bụi chạy ngay (rơi khỏi mép vẫn để bụi tan tự nhiên).
- `init.sh`: thêm reference `Library/ScriptAssemblies/Opsive.*.dll` (task BD nằm ở ProjectReference, không có HintPath).

## 2026-11-03 (và các phiên trước)
- Dựng xong 4 tab: Bản đồ, Vật phẩm, Trang bị, Kĩ năng, cùng các editor builder và bộ import dữ liệu.
- Sửa lỗi không chọn được ô khi `Content` bị tắt lúc vào scene (NullReferenceException trong `GetFocusTarget`). User đã xác nhận trong Play mode.
- Nút Menu của tay cầm đã bật được menu sẵn, không đổi code.

## Việc tiếp theo
- Thanh gợi ý nút (A/B/Điều hướng).
- Bản đồ: sprite marker "!", chức năng nút Y "Dịch chuyển".
- Trang bị: dữ liệu nhóm THIẾT BỊ, ảnh vũ khí, nút "NÂNG CẤP".
