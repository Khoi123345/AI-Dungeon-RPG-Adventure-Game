# Hướng Dẫn & Kết Quả Chuyển Đổi StoryScene sang Nhập Văn Bản Tự Do

Hệ thống `StoryScene` đã được cập nhật thành công để thay thế 3 nút lựa chọn cố định bằng việc cho phép người chơi gõ văn bản tự do để tiếp tục cốt truyện hầm ngục.

---

## Các Tệp Đã Thay Đổi

1. **[StoryView.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Scripts/Story/StoryView.cs)**
   - Thêm các thuộc tính `[SerializeField]`: `panelTextInput`, `inputStoryAction` (`TMP_InputField`), `btnSubmitAction` (`Button`).
   - Thêm các phương thức: `SetInputPanelVisible`, `BindSubmitAction`, `GetInputValue`, `ClearInputField`, `SetInputInteractable`.

2. **[StoryPresenter.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Scripts/Story/StoryPresenter.cs)**
   - Lắng nghe sự kiện gửi câu từ `StoryView`.
   - Tự động bật `Panel_TextInput` khi đọc xong văn bản hiện tại.
   - Khi người chơi gõ xong và gửi, hiển thị câu thoại của bạn (ví dụ: `> Bạn: "..."`) lên Story Log, sau đó chuyển giao văn bản đến `GameProgressService` để sinh diễn biến tiếp theo và gõ chữ hiệu ứng (typing effect).

3. **[GameProgressService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Scripts/Core/GameProgressService.cs)**
   - Thêm phương thức `ExecuteCustomStoryAction(string playerInput)` lưu vết lịch sử `StoryAction` với `playerInput`.
   - Sinh phản hồi dynamic câu chuyện tiếp nối với hành động mà người chơi đã nhập.

---

## Hướng Dẫn Gán UI Trong Unity Editor

Vui lòng làm theo các bước sau trong **Unity Editor**:

1. **Mở Scene `StoryScene`** trong Unity Project.
2. Trong cửa sổ **Hierarchy**, chọn GameObject chứa script `StoryView` (ví dụ GameObject `Canvas` hoặc GameObject chứa `StoryView`).
3. Mở cửa sổ **Inspector** của `StoryView (Script)`:
   - Tìm mục **Custom Text Input**:
     - Kéo GameObject `Panel_TextInput` từ Hierarchy vào ô **Panel Text Input**.
     - Kéo Component `TMP_InputField` (nằm trong `Panel_TextInput` hoặc `Scroll View 1`) vào ô **Input Story Action**.
     - Kéo GameObject `btn_OK` (Component `Button`) vào ô **Btn Submit Action**.

![Cấu hình Inspector](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Graphics/Sprites/GameUI/bg1_0.png)

4. **Nhấn Play (▶️)** để thử nghiệm:
   - Đoạn văn mở đầu câu chuyện sẽ chạy hiệu ứng gõ chữ.
   - Ngay khi gõ xong, `Panel_TextInput` sẽ tự động xuất hiện.
   - Bạn gõ hành động mong muốn (ví dụ: *"Tôi dùng kiếm chém vào bức tường đá để tìm lối đi ẩn"*) và ấn **OK** (hoặc gõ phím Enter).
   - Nội dung câu chuyện tiếp theo sẽ phản hồi lại hành động của bạn!
