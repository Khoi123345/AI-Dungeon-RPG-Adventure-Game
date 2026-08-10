# CHAPTER 2: THE SUNKEN KINGDOM (VƯƠNG QUỐC CHÌM ĐẮM)

## CRITICAL PROGRESSION RULES (LUẬT TIẾN TRÌNH BẮT BUỘC)
* **LUẬT CHUYỂN CẢNH CẬP NHẬT ĐỊA ĐIỂM (BẮT BUỘC):**
  1. Khi quét thấy vật phẩm `"item_sea_compass"` trong danh sách `INVENTORY` của người chơi, AI **BẮT BUỘC** phải cung cấp ít nhất 1 Lựa chọn (Choice) để người chơi tiến sang địa điểm tiếp theo: `"label": "Dùng Hải Đồ lặn xuống Rãnh Sâu Vô Tận"`, `"nextNodeId": "abyssal_trench"`.
  2. Khi quét thấy vật phẩm `"item_void_crystal"` trong danh sách `INVENTORY` của người chơi, AI **BẮT BUỘC** phải cung cấp ít nhất 1 Lựa chọn (Choice) để người chơi tiến sang địa điểm tiếp theo: `"label": "Tiến vào Cung Điện San Hô"`, `"nextNodeId": "coral_palace"`.

---

## Description
Từng là một học viện ma thuật vĩ đại, giờ đây toàn bộ vương quốc đã chìm sâu dưới đáy biển do một cơn đại hồng thủy hắc ám. Ma thuật Void hoành hành biến các sinh vật biển và thủy thủ thành oan hồn dữ tợn.

## Locations Included
1. Sunken Shipwreck (Xác Tàu Đắm) - Vị trí 1 (Dễ)
2. Abyssal Trench (Rãnh Sâu Vô Tận) - Vị trí 2 (Trung bình)
3. Coral Palace (Cung Điện San Hô) - Vị trí 3 (Khó - Nơi ở của Boss Shadow Demon)

## Chapter Objective & Linear Story Flow
Người chơi phải tuân theo và hoàn thành các bước cốt truyện sau để tới phòng Boss:

### Bước 1: Khám phá Xác Tàu Đắm (Location: sunken_shipwreck, Node: sunken_shipwreck)
- **Quái vật chạm trán**: Thủy Thủ Chết Đuối (`mob_drowned_sailor`), Cua Đột Biến (`mob_mutated_crab`), Tàn Dư Hư Không (`mob_void_remnant`).
- **Nhiệm vụ & Rớt Đồ**: Đánh bại quái vật hoặc thám hiểm boong tàu để tìm Hải Đồ Biển Sâu (ID: `item_sea_compass`).
- **Chú ý**: Tuân thủ luật chuyển tiếp số 1 ở phần LUẬT TIẾN TRÌNH bên trên khi người chơi nhặt được hải đồ.

### Bước 2: Thám hiểm Rãnh Sâu Vô Tận (Location: abyssal_trench, Node: abyssal_trench)
- **Quái vật chạm trán**: Oan Hồn Biển Sâu (`mob_abyssal_spirit`), Tàn Dư Hư Không (`mob_void_remnant`).
- **Nhiệm vụ & Rớt Đồ**: Tiêu diệt Tàn Dư Hư Không hoặc giải bẫy bóng tối để thu thập Pha Lê Hư Không (ID: `item_void_crystal`).
- **Chú ý**: Tuân thủ luật chuyển tiếp số 2 ở phần LUẬT TIẾN TRÌNH bên trên khi người chơi thu thập được pha lê.

### Bước 3: Tiến vào Cung Điện San Hô (Location: coral_palace, Node: coral_palace -> boss_room)
- **Nhiệm vụ**: Sử dụng Pha Lê Hư Không (ID: `item_void_crystal`) để giải trừ lớp ma thuật Void chắn cổng vào Phòng Ngai Vàng.
- **Tiến trình**:
  1. Ngoài điện: Người chơi vượt qua lính canh Oan Hồn Bóng Tối.
  2. Lối vào Phòng Ngai Vàng: Người chơi chọn "Đối mặt với Shadow Demon" để kích hoạt trận chiến cuối Chương 2.
- **Kích hoạt trận chiến**: Khi người chơi chọn đối đầu Boss tại node `boss_room`, bạn phải đặt `"triggerBattle": true`, `"bossId": "boss_shadow_demon"`, `"bossName": "Shadow Demon"`, `"bossLevel": 20`.