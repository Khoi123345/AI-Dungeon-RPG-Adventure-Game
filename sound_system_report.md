# BÁO CÁO PHÁT TRIỂN TÍNH NĂNG: HỆ THỐNG ÂM THANH TOÀN CỤC VÀ ÂM THANH NÚT BẤM (UI BUTTON SOUND SYSTEM)

## 1. Thông tin chung
* **Tên tính năng:** Hệ thống Quản lý Âm thanh Toàn cục và Âm thanh Nút bấm (Audio & UI Sound System).
* **Nền tảng phát triển:** Unity Engine (C#).
* **Mục tiêu:** 
  * Cung cấp giải pháp phát nhạc nền (BGM) và hiệu ứng âm thanh (SFX) tập trung.
  * Tích hợp âm thanh phản hồi (Feedback Sound) khi người chơi tương tác với các nút bấm trên giao diện người dùng (UI Buttons) mà không làm ảnh hưởng đến luồng logic cũ của game.
  * Cung cấp khả năng kiểm soát âm lượng linh hoạt (tăng/giảm âm lượng trực tiếp từ Editor hoặc lập trình bằng Code).

---

## 2. Kiến trúc và Nguyên lý hoạt động (System Architecture)
Hệ thống được thiết kế theo mô hình tách biệt trách nhiệm (Decoupling) và hướng thành phần (Component-Based), bao gồm hai lớp xử lý chính:

```
[UI Button GameObject] 
        │ (Tự động bắt sự kiện Click qua EventSystem)
        ▼
[PlaySoundOnButton Component] 
        │ (Yêu cầu phát SFX thông qua Singleton)
        ▼
[SoundManager Singleton] 
        │ (Điều chỉnh Volume tương ứng)
        ▼
[sfxSource: AudioSource] ───► [AudioClip: ButtonClick] ───► (Phát ra loa)
```

1. **Lớp Quản lý Tập trung (SoundManager):** Sử dụng thiết kế **Singleton Pattern** để đảm bảo chỉ có duy nhất một thực thể quản lý âm thanh tồn tại xuyên suốt quá trình chạy game (không bị hủy khi chuyển đổi Scene nhờ `DontDestroyOnLoad`). Quản lý hai nguồn phát âm thanh riêng biệt là Nhạc nền (`musicSource`) và Hiệu ứng (`sfxSource`).
2. **Lớp Kích hoạt Âm thanh (PlaySoundOnButton):** Là một thành phần độc lập (Component) gắn trực tiếp vào các nút bấm UI. Nó tự động bắt sự kiện tương tác của người chơi mà không cần can thiệp vào các Script điều khiển UI chính.

---

## 3. Chi tiết triển khai mã nguồn (Implementation Details)

### 3.1. Bộ quản lý âm thanh trung tâm (SoundManager.cs)
* **Vị trí lưu trữ:** `Assets/Script/Core/SoundManager.cs`
* **Nhiệm vụ:** Khởi tạo các `AudioSource` cần thiết, lưu trữ clip âm thanh mặc định, và cung cấp các hàm API công khai (`PlaySFX`, `PlayDefaultClickSound`, `PlayMusic`) để các đối tượng khác gọi phát âm thanh. Đồng thời cung cấp các thuộc tính `SFXVolume` và `MusicVolume` để thay đổi âm lượng.

### 3.2. Thành phần phát âm thanh nút bấm (PlaySoundOnButton.cs)
* **Vị trí lưu trữ:** `Assets/Script/Core/PlaySoundOnButton.cs`
* **Nhiệm vụ:** Lắng nghe sự kiện nhấp chuột của người dùng. Cho phép tùy chỉnh âm thanh riêng biệt cho từng nút nếu muốn (qua trường `customClickSound`).

---

## 4. Giải quyết thách thức kỹ thuật (Key Decision & Troubleshooting)

* **Vấn đề gặp phải:** Trong mã nguồn UI cũ của game (như `WelcomePanelController`, `LoginPanelController`, v.v.), hệ thống sử dụng câu lệnh `button.onClick.RemoveAllListeners()` ở hàm khởi chạy để dọn dẹp các sự kiện cũ. Điều này vô tình xóa luôn sự kiện phát âm thanh được đăng ký động thông qua `button.onClick.AddListener()`.
* **Giải pháp khắc phục:** 
  * Thay vì đăng ký sự kiện qua `button.onClick`, thành phần `PlaySoundOnButton` được nâng cấp để kế thừa interface **`IPointerClickHandler`** từ thư viện `UnityEngine.EventSystems`.
  * Sự kiện click chuột sẽ được gửi trực tiếp từ **EventSystem** của Unity đến phương thức `OnPointerClick(PointerEventData)` của component này.
  * Nhờ đó, tính năng phát âm thanh nút bấm hoạt động **hoàn toàn độc lập** và không bao giờ bị ảnh hưởng bởi các hàm xóa sự kiện `RemoveAllListeners()` của các controller khác, đồng thời vẫn kiểm tra được trạng thái `interactable` của nút bấm để tránh phát âm thanh khi nút đang bị khóa.

---

## 5. Hướng dẫn cấu hình và Sử dụng (User Guide)

### Bước 1: Khởi tạo đối tượng quản lý âm thanh trong Scene
1. Tạo một GameObject rỗng tên là `SoundManager` ở Scene khởi đầu.
2. Gắn script `SoundManager` vào đối tượng này.
3. Kéo thả file âm thanh click chuột mong muốn vào trường **`Default Click Sound`** trong bảng Inspector.

### Bước 2: Thiết lập âm thanh cho các Button trên UI
* **Cách tự động (Khuyên dùng):** Chọn các Button trong Hierarchy, bấm **Add Component** -> tìm và gắn thêm component `Play Sound On Button`. Nút bấm sẽ tự động phát âm thanh mặc định từ `SoundManager`.
* **Cách tùy biến:** Nếu muốn nút bấm phát âm thanh đặc biệt (Ví dụ: Tiếng nhận hòm đồ, tiếng lỗi đăng nhập), kéo thả file âm thanh đó vào ô **`Custom Click Sound`** trên component `Play Sound On Button` của nút đó.

### Bước 3: Điều chỉnh âm lượng (Volume Adjustment)
* **Trong Unity Editor:** Chọn `SoundManager` trên Hierarchy và kéo thanh trượt **`Music Volume`** hoặc **`Sfx Volume`** (từ `0.0` đến `1.0`) trên bảng Inspector. (Hỗ trợ thay đổi thời gian thực ngay khi đang chơi thử game).
* **Bằng Code C# (Mở rộng cho cài đặt Settings):**
  ```csharp
  SoundManager.Instance.SFXVolume = 0.5f;   // Đặt âm lượng SFX ở mức 50%
  SoundManager.Instance.MusicVolume = 0.3f; // Đặt âm lượng nhạc nền ở mức 30%
  ```

---

## 6. Kết quả nghiệm thu (Validation Results)
* [x] Hệ thống âm thanh được khởi tạo chính xác và không bị trùng lặp hay bị hủy khi chuyển Scene.
* [x] Tất cả các nút bấm được gắn thẻ `PlaySoundOnButton` đều phát âm thanh phản hồi chuẩn xác khi click chuột.
* [x] Tránh được lỗi mất tiếng do xung đột sự kiện xóa Listener (`RemoveAllListeners`) trên các panel giao diện cũ.
* [x] Các nút ở trạng thái bị vô hiệu hóa (`interactable = false`) không phát ra âm thanh click không mong muốn.
* [x] Việc tăng/giảm âm lượng hoạt động tốt trên cả thanh trượt Editor Inspector lẫn thông qua thay đổi bằng code.
