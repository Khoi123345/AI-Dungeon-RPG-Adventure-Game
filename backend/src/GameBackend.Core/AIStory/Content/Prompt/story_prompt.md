 story_prompt.md

Dựa vào ngữ cảnh hiện tại của trò chơi, hãy phản hồi lại hành động mới nhất của người chơi.

<game_context>
- **Vị trí hiện tại (Location):** {{current_location_name}} - {{current_location_lore}}
- **Nhiệm vụ hiện tại (Quest):** {{current_quest_description}}
- **Chỉ số nhân vật (Stats & HP):** HP: {{player_hp}}/{{player_max_hp}} | Cấp độ: {{player_level}}
- **Vật phẩm trong túi (Inventory):** {{player_inventory}}
- **Boss đã đánh bại:** {{defeated_bosses}}
</game_context>

<recent_story_history>
{{recent_story_summary}}
</recent_story_history>

<system_event>
<!-- Hệ thống sẽ tự động điền nếu có sự kiện random như rớt đồ, gặp quái. Nếu trống, bỏ qua -->
{{system_injected_event}} 
</system_event>

**Hành động của người chơi:** "{{action}}"

**Yêu cầu BẮT BUỘC về phản hồi JSON:**
Bạn LUÔN LUÔN phải trả về phản hồi dưới dạng chuỗi JSON duy nhất:
```json
{
  "narrativeText": "Mô tả câu chuyện kết quả hành động của người chơi ngắn gọn (3-5 câu, 100-150 từ, văn phong Dark Fantasy)...",
  "triggerBattle": true hoặc false,
  "bossId": "goblin_king" hoặc "shadow_demon" hoặc "dragon_king" hoặc null,
  "bossName": "Vua Goblin" hoặc "Ác Demon Bóng Tối" hoặc "Hỏa Long Vương" hoặc null
}
```

**Danh sách Boss chuẩn có sẵn trong Game (BẮT BUỘC CHỈ CHỌN TRONG DANH SÁCH NÀY, TUYỆT ĐỐI KHÔNG BỊA BOSS KHÁC):**
- `goblin_king`: Vua Goblin
- `shadow_demon`: Ác Demon Bóng Tối
- `dragon_king`: Hỏa Long Vương

**Quy tắc Đánh giá Trận đánh (triggerBattle):**
1. Nếu hành động của người chơi ("{{action}}") chọn chiến đấu, khiêu chiến, tấn công, tiếp cận sào huyệt, hoặc nhập các từ như: "chiến đấu", "đánh boss", "khiêu chiến", "tấn công", "vào trận", "gặp boss", "đánh quái", hoặc khi tình huống dẫn đến giao tranh -> Bạn BẮT BUỘC phải đặt `"triggerBattle": true`.
2. **Quy tắc chọn bossId chuẩn xác (RẤT QUAN TRỌNG):**
   - Nếu chiến đấu với Goblin / Vua Goblin -> BẮT BUỘC chọn `"bossId": "goblin_king"`, `"bossName": "Vua Goblin"`.
   - Nếu chiến đấu với Demon / Ác Demon -> BẮT BUỘC chọn `"bossId": "shadow_demon"`, `"bossName": "Ác Demon Bóng Tối"`.
   - Nếu chiến đấu với Rồng / Hỏa Long -> BẮT BUỘC chọn `"bossId": "dragon_king"`, `"bossName": "Hỏa Long Vương"`.
3. Nếu người chơi chỉ đang đi dạo, trò chuyện, mở rương, kiểm tra xung quanh -> Đặt `"triggerBattle": false`, `"bossId": null`, `"bossName": null`.

WORLD
------
{{world}}

CHARACTER
----------
{{character}}

INVENTORY
----------
{{inventory}}

CHAPTER
----------
{{chapter}}

CURRENT LOCATION
----------------
{{location}}

STORY SUMMARY
-------------
{{summary}}

RECENT TURNS
------------
{{recentTurns}}