# LOCATION: DRAGON NEST (Chapter 3 - Location 3 - Boss Location)

## Lore
Nằm trên đỉnh cao nhất của Ngọn núi Hắc Diệu Thạch, hang ổ khổng lồ chứa hàng triệu đồng kim cương vàng bạc bị ô nhiễm bởi ma thuật rồng. Đây là nơi ngự trị của Vua Rồng (Dragon King) — chúa tể rực lửa của Vùng Đất Hoang Tàn.

## Environment & Atmosphere
- Sông nham thạch nóng chảy sục sôi dưới chân. Tiếng kim loại của kho báu rượt đuổi trong sức nóng hầm hập.
- Bầu không khí tràn ngập long uy, mỗi bước chân đều làm rung chuyển vòm hang vĩ đại.

## Quái vật tiềm năng
- Adult Dragon (Rồng Trưởng Thành) - ID: `mob_adult_dragon`
- Young Dragon (Rồng Trẻ) - ID: `mob_young_dragon`

## Gameplay Prompts for AI
- **Encounters:** Rồng Trưởng Thành (`mob_adult_dragon`), Rồng Trẻ (`mob_young_dragon`).
- **Quest Item Required:** Người chơi dùng Chìa Khóa Long Huyết (`item_dragon_blood_key`) để mở cánh cửa đại bàng dẫn vào Cung Điện Vua Rồng. Khi đứng trước cửa, AI **BẮT BUỘC** cung cấp lựa chọn: `"label": "Đối mặt với Dragon King"`, `"nextNodeId": "boss_room"`.
- **Boss Room Trigger:** Khi `currentNodeId` là `boss_room`, AI phải đặt `"triggerBattle": true`, `"bossId": "boss_dragon_king"`, `"bossName": "Dragon King"`, `"bossLevel": 25`.
- **Return Route:** Người chơi luôn có thể chọn rút lui về Đỉnh Núi Hắc Diệu Thạch (`"label": "Rút lui về Đỉnh Núi Hắc Diệu Thạch"`, `"nextNodeId": "obsidian_peaks"`) để luyện cấp khi chưa đủ sức thắng Boss Dragon King.
- **Narrative Detail:** Mô tả sự xuất hiện vô cùng uy nghi, rung chuyển đất trời của Dragon King — ngọn lửa thần thoại bùng cháy trên đôi cánh rồng khổng lồ.