using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggle : MonoBehaviour
{
    [Header("Thanh Nhập Mật Khẩu")]
    [SerializeField] private TMP_InputField passwordInputField; // Khai báo ô mật khẩu

    [Header("Hình Ảnh Của Nút Con Mắt")]
    [SerializeField] private Image eyeButtonImage;             // Khai báo component Image để đổi icon

    [Header("Bộ Icon Thay Đổi (Sprites)")]
    [SerializeField] private Sprite eyeCloseSprite; // Icon Ổ khóa (Khi ẩn mật khẩu)
    [SerializeField] private Sprite eyeOpenSprite;  // Icon mở khóa/Mắt mở (Khi hiện mật khẩu)

    private bool isPasswordShowing = false; // Biến kiểm tra trạng thái (Đang hiện hay đang ẩn)

    private void Start()
    {
        // 1. Khi game vừa chạy, ép ô nhập liệu phải ẩn mật khẩu ở dạng dấu *
        if (passwordInputField != null)
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
            passwordInputField.ForceLabelUpdate(); // Ép giao diện cập nhật ngay
        }
        
        // 2. Ép nút bấm phải hiển thị hình ảnh mặc định là Ổ khóa đóng (eyeCloseSprite)
        if (eyeButtonImage != null && eyeCloseSprite != null)
        {
            eyeButtonImage.sprite = eyeCloseSprite;
        }
    }

    // Hàm xử lý sự kiện khi người chơi CLICK vào nút con mắt
    public void TogglePasswordVisibility()
    {
        if (passwordInputField == null) return;

        // Đảo ngược trạng thái: Nếu đang ẩn (false) thì thành hiện (true), và ngược lại
        isPasswordShowing = !isPasswordShowing;

        if (isPasswordShowing)
        {
            // Nếu bấm để HIỆN mật khẩu:
            passwordInputField.contentType = TMP_InputField.ContentType.Standard; // Chuyển kiểu hiển thị về chữ thường
            if (eyeOpenSprite != null) eyeButtonImage.sprite = eyeOpenSprite;     // Đổi icon sang hình mở khóa
        }
        else
        {
            // Nếu bấm để ẨN mật khẩu:
            passwordInputField.contentType = TMP_InputField.ContentType.Password; // Chuyển kiểu hiển thị về dạng dấu *
            if (eyeCloseSprite != null) eyeButtonImage.sprite = eyeCloseSprite;    // Đổi icon về hình ổ khóa đóng
        }

        // Bắt buộc ô nhập liệu vẽ lại chữ mới ngay lập tức lên màn hình game
        passwordInputField.ForceLabelUpdate();
    }
}