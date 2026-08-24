# LOCATION: ABYSSAL TRENCH (Chapter 2 - Location 2)

## Lore
Rãnh sâu vô tận nằm dưới đáy của Vương Quốc Chìm Đắm — nơi ánh sáng không bao giờ chạm tới. Áp suất nước ở đây đủ để nghiền nát giáp trụ thông thường. Những thực thể Void cổ xưa ngủ vùi trong bóng tối, thi thoảng thức giấc để săn mồi bất kỳ kẻ nào dám xâm nhập.

## Environment & Atmosphere
- Bóng tối tuyệt đối, chỉ có ánh phát quang kỳ dị từ các sinh vật biển đột biến.
- Tiếng vang vọng kỳ lạ, như tiếng thì thầm của những linh hồn bị nhốt dưới đáy.
- Dòng nước lạnh buốt và những túi khí Void độc hại rải rác khắp nơi.

## Gameplay Prompts for AI
- **Encounters:** Tàn Dư Hư Không (`mob_void_remnant`), Oan Hồn Biển Sâu (`mob_abyssal_spirit`), Thủy Thủ Chết Đuối (`mob_drowned_sailor`).
- **Quest Item Drop:** Người chơi tiêu diệt Tàn Dư Hư Không hoặc khám phá túi khí Void sẽ nhận được Pha Lê Hư Không (ID: `item_void_crystal`). Hãy tạo item này trong `inventoryChanges`: `[{ "itemId": "item_void_crystal", "quantityDelta": 1 }]`.
- **Navigation & Progression:** Khi người chơi có `item_void_crystal`, AI **BẮT BUỘC** đưa ra lựa chọn tiến vào Cung Điện San Hô: `"label": "Tiến vào Cung Điện San Hô"`, `"nextNodeId": "coral_palace"`.
- **Return Route:** Người chơi luôn có thể chọn quay lại Xác Tàu Đắm (`"nextNodeId": "sunken_shipwreck"`).
