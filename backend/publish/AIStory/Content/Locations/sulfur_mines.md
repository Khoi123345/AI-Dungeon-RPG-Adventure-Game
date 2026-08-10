# LOCATION: SULFUR MINES (Chapter 3 - Location 1)

## Lore
Nằm ở chân Vùng Đất Hoang Tàn, mỏ lưu huỳnh cổ đại bị bao phủ bởi sương độc vàng óng và những suối nham thạch cuồn cuộn. Khói độc bốc lên nghi ngút làm mờ tầm nhìn, trong khi tiếng gầm rú nung nấu của núi lửa vang lên rùng rợn.

## Environment & Atmosphere
- Không khí nóng ngột ngạt, sương lưu huỳnh bốc lên cuồn cuộn.
- Tiếng vỡ nứt của bẫy đá nham thạch và ánh sáng đỏ rực từ lòng đất.

## Quái vật tiềm năng
- Fire Lizard (Khủng Long Lửa) - ID: `mob_fire_lizard`
- Young Dragon (Rồng Trẻ) - ID: `mob_young_dragon`

## Gameplay Prompts for AI
- **Narrative Tone:** Dẫn dắt bầu không khí rùng rợn, hầm hập sức nóng. Miêu tả chi tiết cảm giác không khí nóng rát và ánh lửa nham thạch soi rọi các tàn tích.
- **Quest Item Drop:** Khi người chơi thám hiểm hoặc tiêu diệt quái vật, hãy trao Chìa Khóa Hắc Diệu Thạch (ID: `item_obsidian_key`) trong `inventoryChanges`: `[{ "itemId": "item_obsidian_key", "quantityDelta": 1 }]`.
- **Navigation:** Khi có `item_obsidian_key`, AI **BẮT BUỘC** đưa ra lựa chọn: `"label": "Trèo lên Đỉnh Núi Hắc Diệu Thạch"`, `"nextNodeId": "obsidian_peaks"`.
