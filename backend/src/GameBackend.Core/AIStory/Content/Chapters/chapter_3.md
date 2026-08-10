# CHAPTER 3: THE SCORCHED WASTELAND (VÙNG ĐẤT HOANG TÀN RỰC LỬA)

## CRITICAL PROGRESSION RULES (LUẬT TIẾN TRÌNH BẮT BUỘC)
* **LUẬT CHUYỂN CẢNH CẬP NHẬT ĐỊA ĐIỂM (BẮT BUỘC):**
  1. Khi quét thấy vật phẩm `"item_obsidian_key"` trong danh sách `INVENTORY` của người chơi, AI **BẮT BUỘC** phải cung cấp ít nhất 1 Lựa chọn (Choice) để người chơi tiến sang địa điểm tiếp theo: `"label": "Trèo lên Đỉnh Núi Hắc Diệu Thạch"`, `"nextNodeId": "obsidian_peaks"`.
  2. Khi quét thấy vật phẩm `"item_dragon_blood_key"` trong danh sách `INVENTORY` của người chơi, AI **BẮT BUỘC** phải cung cấp ít nhất 1 Lựa chọn (Choice) để người chơi tiến sang địa điểm tiếp theo: `"label": "Tiến vào Tổ Rồng"`, `"nextNodeId": "dragon_nest"`.

---

## Description
Một vùng đất cổ xưa bị thiêu rụi bởi dòng nham thạch nóng chảy và tro tàn ngút trời. Sức nóng hầm hập có thể làm tan chảy giáp trụ thông thường. Nơi đây từng là thánh địa của các loài rồng cổ xưa, giờ đây bị chiếm đóng bởi Dragon King — thực thể tàn bạo đe dọa thiêu rụi toàn bộ thế giới.

## Locations Included
1. Sulfur Mines (Mỏ Lưu Huỳnh) - Vị trí 1 (Dễ)
2. Obsidian Peaks (Đỉnh Núi Hắc Diệu Thạch) - Vị trí 2 (Trung bình)
3. Dragon Nest (Tổ Rồng) - Vị trí 3 (Khó - Nơi ở của Boss Dragon King)

## Chapter Objective & Linear Story Flow
Người chơi phải tuân theo và hoàn thành các bước cốt truyện sau để tới phòng Boss:

### Bước 1: Vượt qua Mỏ Lưu Huỳnh (Location: sulfur_mines, Node: sulfur_mines)
- **Quái vật chạm trán**: Khủng Long Lửa (ID: `mob_fire_lizard`), Rồng Trẻ (ID: `mob_young_dragon`).
- **Nhiệm vụ & Rớt Đồ**: Sử dụng Lõi Lửa Kháng Nhiệt (`item_fire_core`) từ Chapter 2 để chịu đựng sức nóng, đồng thời thám hiểm hoặc đánh quái để thu thập Chìa Khóa Hắc Diệu Thạch (ID: `item_obsidian_key`).
- **Chú ý**: Tuân thủ luật chuyển tiếp số 1 ở phần LUẬT TIẾN TRÌNH bên trên khi người chơi nhặt được chìa khóa.

### Bước 2: Chinh phục Đỉnh Núi Hắc Diệu Thạch (Location: obsidian_peaks, Node: obsidian_peaks)
- **Quái vật chạm trán**: Khủng Long Săn Lửa (ID: `mob_fire_raptor`), Rồng Trưởng Thành (ID: `mob_adult_dragon`).
- **Nhiệm vụ & Rớt Đồ**: Vượt qua các vách đá nham thạch và tiêu diệt Khủng Long Săn Lửa để thu thập Chìa Khóa Long Huyết (ID: `item_dragon_blood_key`).
- **Chú ý**: Tuân thủ luật chuyển tiếp số 2 ở phần LUẬT TIẾN TRÌNH bên trên khi người chơi thu thập được chìa khóa.

### Bước 3: Đột kích Tổ Rồng (Location: dragon_nest, Node: dragon_nest -> boss_room)
- **Nhiệm vụ**: Sử dụng Chìa Khóa Long Huyết (ID: `item_dragon_blood_key`) để mở cánh cửa đá nguyên khối dẫn vào Cung Điện Vua Rồng.
- **Tiến trình**:
  1. Hành lang sương lửa: Vượt qua sự ngăn cản của Rồng Trưởng Thành (`mob_adult_dragon`).
  2. Lối vào Cung Điện Vua Rồng: Người chơi bắt buộc chọn "Mở cửa bằng Chìa Khóa Long Huyết" để bước vào `boss_room`.
- **Kích hoạt trận chiến**: Khi người chơi chọn đối đầu Boss tại node `boss_room`, bạn phải đặt `"triggerBattle": true`, `"bossId": "boss_dragon_king"`, `"bossName": "Dragon King"`, `"bossLevel": 25`.