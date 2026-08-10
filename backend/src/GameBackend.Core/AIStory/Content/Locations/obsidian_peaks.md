# LOCATION: OBSIDIAN PEAKS (Chapter 3 - Location 2)

## Lore
Những đỉnh núi làm từ đá hắc diệu thạch nhọn trắt, vươn cao như những chiếc răng nanh của quỷ dữ. Nơi đây gió lốc mang theo tro lửa hầm hập, và những con rồng săn mồi rình rập trên các vách đá sừng sững.

## Environment & Atmosphere
- Đá đen bóng loáng phản chiếu ánh lửa đỏ thẫm.
- Tiếng vỗ cánh cuồn cuộn của loài rồng và tiếng rên xiết của gió núi nóng cháy.

## Quái vật tiềm năng
- Fire Raptor (Khủng Long Săn Lửa) - ID: `mob_fire_raptor`
- Adult Dragon (Rồng Trưởng Thành) - ID: `mob_adult_dragon`

## Gameplay Prompts for AI
- **Narrative Tone:** Tăng tính kịch tính, nhấn mạnh địa hình nguy hiểm trên vách đá cao chót vót và nguy cơ bị tấn công bất ngờ từ không trung.
- **Quest Item Drop:** Người chơi đánh bại Khủng Long Săn Lửa (`mob_fire_raptor`) sẽ thu thập được Chìa Khóa Long Huyết (ID: `item_dragon_blood_key`). Hãy tạo item này trong `inventoryChanges`: `[{ "itemId": "item_dragon_blood_key", "quantityDelta": 1 }]`.
- **Navigation:** Khi có `item_dragon_blood_key`, AI **BẮT BUỘC** đưa ra lựa chọn: `"label": "Tiến vào Tổ Rồng"`, `"nextNodeId": "dragon_nest"`.
- **Return Route:** Người chơi luôn có thể chọn quay lại Mỏ Lưu Huỳnh (`"label": "Quay lại Mỏ Lưu Huỳnh"`, `"nextNodeId": "sulfur_mines"`).
