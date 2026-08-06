# story_prompt.md

Dựa vào ngữ cảnh hiện tại của trò chơi, hãy phản hồi lại hành động mới nhất của người chơi.

**Hành động mới nhất của người chơi:** "{{action}}"

---

### QUY TẮC ĐÁNH GIÁ TRẬN ĐÁNH & PHẢN HỒI:

1. **TUYỆT ĐỐI KHÔNG BỊA QUÁI VẬT KHÔNG CÓ TRONG CỐT TRUYỆN:**
   - **NGHIÊM CẤM** tự bịa ra nhện độc khổng lồ hay quái phụ ngoài luồng. Ở Chương 1 (`ancient_cave`), kẻ thù duy nhất là **Băng nhóm Goblin & Vua Goblin (`goblin_king`)**.

2. **DẪN DẮT CỐT TRUYỆN & KÍCH HOẠT TRẬN ĐÁNH TỰ ĐỘNG (POKEMON-STYLE):**
   - Hãy chủ động dẫn dắt diễn biến câu chuyện khi người chơi bước vào hang động, di chuyển hoặc lục soát.
   - Mô tả ngay Vua Goblin (hoặc toán tay sai Goblin dưới chướng Vua Goblin) phục kích chặn đường và **BẮT BUỘC ĐẶT `"triggerBattle": true` NGAY TẠI LƯỢT ĐÓ!** (Không cần chờ người chơi gõ chữ "tấn công" hay "chiến đấu").
   - **Gán BossId Chuẩn theo Chương hiện tại:**
     - Chương 1 ➔ `"bossId": "goblin_king"`, `"bossName": "Vua Goblin"`
     - Chương 2 ➔ `"bossId": "shadow_demon"`, `"bossName": "Ác Demon Bóng Tối"`
     - Chương 3 ➔ `"bossId": "dragon_king"`, `"bossName": "Hỏa Long Vương"`
     - TUYỆT ĐỐI KHÔNG bịa bossId nào khác ngoài 3 ID trên!

3. **QUY TẮC CHỐNG NHẢY CHƯƠNG / NHẢY BOSS (ANTI-SEQUENCE BREAKING):**
   - Nếu người chơi ở Chương 1 mà nhắn đòi gặp Boss Chương 2/Chương 3 hoặc đòi qua Chương mới:
   - AI **BẮT BUỘC TỪ CHỐI** nhập vai (ví dụ: sương độc/rào cản ma thuật của Vua Goblin phong tỏa, bắt buộc phải hạ Vua Goblin ở Chương 1 trước).

---

**Danh sách Boss chuẩn có sẵn trong Game (BẮT BUỘC CHỈ CHỌN TRONG DANH SÁCH NÀY, TUYỆT ĐỐI KHÔNG BỊA BOSS KHÁC):**
- `goblin_king`: Vua Goblin
- `shadow_demon`: Ác Demon Bóng Tối
- `dragon_king`: Hỏa Long Vương

**Yêu cầu BẮT BUỘC về phản hồi JSON:**
Bạn LUÔN LUÔN phải trả về phản hồi dưới dạng chuỗi JSON duy nhất:
```json
{
  "narrativeText": "Mô tả câu chuyện kết quả hành động của người chơi ngắn gọn (3-5 câu, 100-150 từ, văn phong Dark Fantasy lôi cuốn)...",
  "triggerBattle": true hoặc false,
  "bossId": "goblin_king" hoặc "shadow_demon" hoặc "dragon_king" hoặc null,
  "bossName": "Vua Goblin" hoặc "Ác Demon Bóng Tối" hoặc "Hỏa Long Vương" hoặc null
}
```

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