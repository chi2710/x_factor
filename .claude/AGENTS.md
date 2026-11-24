# Agent Instructions — Sample_2D (Unity 6000.5)

## Trước khi bắt đầu
1. Chạy `bash .claude/init.sh`: kiểm tra Unity/Roslyn và biên dịch thử `Assets/Resources/Scripts`. Phải ra **0 lỗi** trước khi sửa gì.
   - Nếu báo thiếu `.csproj`: mở Unity > Preferences > External Tools > Regenerate project files.
2. Đọc `session-handoff.md` (bối cảnh, quyết định đã chốt), `claude-progress.md` (nhật ký phiên) và `feature_list.json` (việc đã xong / sắp làm).

## Quy tắc
- Làm đúng **một** tính năng mỗi lần.
- **Luôn đọc lại file trước khi sửa**: user thường tự sửa file trong Unity giữa các lượt. Không bao giờ ghi đè file chưa đọc.
- Không chạy lại các menu Tools > Menu UI > Setup/Build… nếu chưa hỏi user — chúng ghi đè chỉnh sửa tay trong scene.
- Không sửa file `.unity`, `.prefab`, `.asset`, `.meta` bằng tay trừ khi user yêu cầu; dùng script Editor hoặc hướng dẫn user thao tác trong Unity.
- Không tuyên bố "xong" khi chưa biên dịch 0 lỗi; với thay đổi hành vi, ghi rõ là **chưa kiểm tra Play mode** cho đến khi user xác nhận.
- Cuối mỗi phiên: cập nhật `claude-progress.md` và `session-handoff.md`; cập nhật `feature_list.json` khi trạng thái tính năng thay đổi.

## Checklist xác nhận
- [ ] `bash .claude/init.sh` báo 0 lỗi biên dịch
- [ ] Không có warning mới do thay đổi của mình
- [ ] User đã kiểm tra trong Play mode (chuột, bàn phím, tay cầm nếu liên quan)
- [ ] Tính năng chạy đúng như mô tả
