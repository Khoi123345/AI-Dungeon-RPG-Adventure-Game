# LOCATION: CORAL PALACE (Chapter 2 - Location 3 - Boss Location)

## Lore
Từng là cung điện huy hoàng của Vương Quốc San Hô dưới lòng đại dương, nay bị một thực thể Void cổ xưa — Shadow Demon — chiếm đóng và làm ô nhiễm toàn bộ nguồn nước. San hô đỏ thẫm phủ kín các bức tường như những vết thương rỉ máu. Đây là nơi tập trung năng lượng Void mạnh nhất.

## Environment & Atmosphere
- Ánh sáng tím kỳ dị phát ra từ các tảng san hô bị Void hóa.
- Không khí nặng nề, mỗi hơi thở như nuốt phải sương mù hắc ám.
- Bóng tối di chuyển như sinh vật, che khuất lối đi và gây mất phương hướng.

## Gameplay Prompts for AI
- **Encounters:** Oan Hồn Bóng Tối (`mob_shadow_spirit`), Tàn Dư Hư Không (`mob_void_remnant`). Lính canh ngăn cản dữ dội.
- **Quest Item Required:** Người chơi dùng `item_void_crystal` để mở cổng vào Phòng Ngai Vàng. Khi đứng trước cổng, AI **BẮT BUỘC** cung cấp lựa chọn: `"label": "Đối mặt với Shadow Demon"`, `"nextNodeId": "boss_room"`.
- **Boss Room Trigger:** Khi `currentNodeId` là `boss_room`, AI phải đặt `"triggerBattle": true`, `"bossId": "boss_shadow_demon"`, `"bossName": "Shadow Demon"`, `"bossLevel": 20`.
- **Return Route:** Người chơi luôn có thể chọn rút lui về Rãnh Sâu Vô Tận (`"label": "Rút lui về Rãnh Sâu Vô Tận"`, `"nextNodeId": "abyssal_trench"`) để luyện cấp khi chưa sẵn sàng đánh Boss.
- **Victory Reward:** Chỉ khi đánh bại chính xác Shadow Demon, hệ thống Battle backend mới cấp Lõi Lửa Kháng Nhiệt (`item_fire_core`) để mở đường sang Chương 3 (`sulfur_mines`). AI tuyệt đối không tự thêm vật phẩm này vào `inventoryChanges`, và quái thường trong Cung Điện không được làm rơi nó.
