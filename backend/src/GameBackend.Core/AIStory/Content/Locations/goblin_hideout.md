# LOCATION: GOBLIN HIDEOUT (Chapter 1 - Location 3)

## Lore
Sào huyệt ẩn sâu dưới gốc cây cổ thụ mục rữa trong Rừng Thì Thầm. Cả một mạng lưới hang ngầm chật hẹp được đào bới bởi bầy Goblin hung hãn, dẫn đến phòng ngai vàng của Goblin King. Mùi khói lửa và thịt cháy xém bốc lên khắp nơi.

## Environment & Atmosphere
- Hành lang hẹp, trần thấp, ánh đuốc dầu le lói chập chờn.
- Tiếng hú hét, cãi vã của bầy Goblin vang vọng khắp các ngóc ngách.
- Bẫy chông, hố đất, và lưới gài đầy rẫy dọc lối đi.

## Gameplay Prompts for AI
- **Encounters:** Vệ Binh Goblin (`mob_goblin_guard`), Trinh Sát Goblin (`mob_goblin_scout`). Người chơi bắt buộc phải chiến đấu hoặc lén lút vượt qua lính canh trước cổng.
- **Quest Item Required:** Người chơi cần có `item_elemental_core` để mở cánh cửa đá dẫn vào Phòng Ngai Vàng. Khi người chơi đã vào sâu bên trong, AI **BẮT BUỘC** phải cung cấp lựa chọn: `"label": "Đối mặt với Goblin King"`, `"nextNodeId": "boss_room"`. 
- **Boss Room:** Khi `currentNodeId` là `boss_room`, AI phải đặt `"triggerBattle": true`, `"bossId": "boss_goblin_king"`, `"bossName": "Goblin King"`, `"bossLevel": 10`. 
- **Atmosphere Detail:** Mô tả lán trại bừa bộn, kho vũ khí thô sơ, và tiếng reo hò man rợ của bầy lính khi chúng phát hiện ra kẻ xâm nhập. 
