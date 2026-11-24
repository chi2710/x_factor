# Lessons

## Yêu cầu "trả lại" mơ hồ thì hỏi trước (2026-11-07)

- **Chuyện gì xảy ra:** user viết "Trả lại config jump trước đây". Claude tự đoán là hoàn tác logic land trong `Player.cs`, nhưng user muốn giữ logic đó, nên phải áp lại.
- **Quy tắc:**
  - Khi user yêu cầu hoàn tác mà có nhiều thứ có thể bị hoàn tác (clip animation, logic `Player.cs`, thông số Inspector), hỏi rõ phần nào trước khi sửa.
  - Hoàn tác code là thao tác dễ làm, nhưng hoàn tác nhầm khiến user mất một lượt để sửa lại.
