# CHAPTER 1: THE WHISPERING WOODS (RỪNG THÌ THẦM)

## Description
Một khu rừng từng tràn đầy sự sống giờ đây bị bao phủ bởi sương mù hắc ám. Cây cối biết thì thầm những lời nguyền rủa, và các sinh vật nhỏ bé đã biến thành quái vật hung tợn.

## Locations Included
1. Ancient Cave (Hang Động Cổ Xưa) - Vị trí 1 (Dễ)
2. Forgotten Temple (Đền Thờ Bị Lãng Quên) - Vị trí 2 (Trung bình)
3. Goblin Hideout (Sào Huyệt Goblin) - Vị trí 3 (Khó - Nơi ở của Boss)

## Chapter Objective & Linear Story Flow
Người chơi phải tuân theo và hoàn thành các bước cốt truyện sau để tới phòng Boss:

### Bước 1: Khởi đầu tại Hang Động Cổ Xưa (Location: ancient_cave, Node: introduction -> cave_exploration)
- **Nhiệm vụ**: Tìm kiếm chiếc Chìa Khóa Cổ Xưa bị chôn giấu trong hang động. Khi người chơi tìm thấy nó, bạn phải thêm vào túi đồ của họ vật phẩm có ID chính xác là `"item_ancient_key"`.
- **Tiến trình**: Người chơi cần thực hiện ít nhất 2 hành động khám phá hoặc chiến đấu tại đây. Khi tìm thấy Chìa Khóa Cổ Xưa (ID: `item_ancient_key`), người chơi sẽ mở khóa lựa chọn di chuyển đến "Đền Thờ Bị Lãng Quên".
- **Lựa chọn để tiến bước**: Lựa chọn tiếp theo phải có `"nextNodeId": "forgotten_temple"`.

### Bước 2: Khám phá Đền Thờ Bị Lãng Quên (Location: forgotten_temple, Node: forgotten_temple)
- **Nhiệm vụ**: Sử dụng Chìa Khóa Cổ Xưa (ID: `item_ancient_key`) trên Bàn Thờ Nguyên Tố để giải mã năng lượng. Bạn phải tiêu hao chiếc chìa khóa bằng cách trừ đi 1 vật phẩm `"item_ancient_key"` khỏi túi đồ của họ. 
- **Phần thưởng**: Khi kích hoạt thành công Bàn Thờ, người chơi sẽ nhận được Mảnh Vỡ Lõi Nguyên Tố có ID chính xác là `"item_elemental_core"`.
- **Lựa chọn để tiến bước**: Lựa chọn tiếp theo phải có `"nextNodeId": "goblin_hideout"`.

### Bước 3: Đột kích Sào Huyệt Goblin (Location: goblin_hideout, Node: goblin_hideout -> boss_room)
- **Nhiệm vụ**: Sử dụng Mảnh Vỡ Lõi Nguyên Tố (ID: `item_elemental_core`) để mở cánh cửa đá chắn đường vào phòng ngai vàng, tiêu hao 1 vật phẩm `"item_elemental_core"` khỏi hành trang.
- **Tiến trình**: 
  1. Ngoài cổng: Người chơi chiến đấu hoặc lén lút vượt qua lính gác ngoài cổng.
  2. Lối vào Phòng Ngai Vàng: Người chơi bắt buộc phải chọn "Đối mặt với Goblin King" để kích hoạt trận chiến cuối cùng của Chapter.
- **Kích hoạt trận chiến**: Khi người chơi chọn đối đầu Boss tại node `boss_room`, bạn phải đặt `"triggerBattle": true`, `"bossId": "boss_goblin_king"`, `"bossName": "Goblin King"`, `"bossLevel": 10`.