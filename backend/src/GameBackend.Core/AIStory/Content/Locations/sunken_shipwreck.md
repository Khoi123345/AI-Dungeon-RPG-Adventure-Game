# LOCATION: SUNKEN SHIPWRECK (Chapter 2 - Location 1)

## Lore
Nằm ở rìa của Vương Quốc Chìm Đắm, hàng loạt những con tàu cổ xưa bị bóng tối ngấu nghiến. Mùi muối biển mặn chát hòa quyện với tử khí. Từng mảng gỗ mục nát kêu cót két trong dòng nước ngầm lạnh lẽo. Ánh sáng le lói từ những loại rong rêu kỳ dị là thứ duy nhất thắp sáng nơi này.

## Environment & Atmosphere
- Ngột ngạt, cô độc, và rợn người. Tiếng nước chảy róc rách xen lẫn những tiếng động ma quái phát ra từ trong thân tàu.

## Quái vật tiềm năng
- Void Remnant (Tàn Dư Hư Không) - ID: `mob_void_remnant`
- Shadow Spirit (Oan Hồn Bóng Tối) - ID: `mob_shadow_spirit`
- Abyssal Spirit (Oan Hồn Biển Sâu) - ID: `mob_abyssal_spirit`
- Drowned Sailor (Thủy Thủ Chết Đuối) - ID: `mob_drowned_sailor`
- Mutated Crab (Cua Đột Biến) - ID: `mob_mutated_crab`

## Gameplay Prompts for AI
- **Quest Item Drop:** Người chơi đánh bại quái vật hoặc tìm kiếm các rương cổ trong tàu sẽ tìm thấy Hải Đồ Biển Sâu (ID: `item_sea_compass`). Hãy tạo item này trong `inventoryChanges`: `[{ "itemId": "item_sea_compass", "quantityDelta": 1 }]`.
- **Navigation & Progression:** Khi người chơi đã có `item_sea_compass`, AI **BẮT BUỘC** đưa ra lựa chọn lặn xuống Rãnh Sâu: `"label": "Dùng Hải Đồ lặn xuống Rãnh Sâu Vô Tận"`, `"nextNodeId": "abyssal_trench"`.
- **Secret Rooms:** Nếu người chơi thám hiểm phòng phụ (`secret_room`), sau 1 lượt thám hiểm AI phải cho tùy chọn quay lại boong tàu. CHỈ khi đã sở hữu `item_sea_compass` mới được đưa ra tùy chọn tiến sang `abyssal_trench`.
