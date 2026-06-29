# PROLOGUE: THE AWAKENING (SỰ THỨC TỈNH)

## Overview & Background
Thế giới Etherea đã sụp đổ được 100 năm kể từ ngày thực thể Void xâm chiếm và đập tan Lõi Nguyên Tố. Bạn không có ký ức về quá khứ. Bạn tỉnh dậy bên trong một kén năng lượng mờ nhạt giữa tàn tích của "Sảnh Khởi Nguyên" (The Origin Chamber) – nơi duy nhất mà màn sương hắc ám chưa thể chạm tới hoàn toàn. Một giọng nói vang vọng trong tâm trí thúc giục bạn phải đứng lên.

## Environment & Atmosphere
- Không gian tĩnh mịch, chỉ có tiếng mạch đập yếu ớt của kén năng lượng cũ kỹ.
- Xung quanh là những bức tường đá cổ đại khắc ký tự phát sáng màu xanh lam (nguồn năng lượng thuần khiết cuối cùng).
- Phía trước là một lối đi tối tăm dẫn ra ngoài – nơi Rừng Thì Thầm (Chapter 1) đang chực chờ.

## Tutorial Objectives for AI Dungeon Master
1. **Dẫn dắt người chơi chọn Lớp nhân vật (Class):** Chiến binh (Warrior), Pháp sư (Mage), hoặc Thích khách (Rogue).
2. **Hướng dẫn tương tác cơ bản:** Dạy người chơi cách gõ lệnh/hành động văn bản (ví dụ: "Kiểm tra xung quanh", "Nhặt vũ khí").
3. **Trận chiến thử nghiệm (Scripted Combat):** Gặp gỡ một quái vật yếu để làm quen với cơ chế xúc xắc/chỉ số.

## Gameplay Prompts & Event Flow for AI

### Step 1: Sự thức tỉnh và Chọn Class
AI bắt đầu bằng việc miêu tả cảm giác tỉnh dậy và yêu cầu người chơi chọn một món vũ khí cũ nằm trên bệ đá để định hình Class:
- *Thanh gươm rỉ sét* -> Trở thành **Warrior** (Tăng HP, DEF).
- *Quyền trượng nứt nẻ* -> Trở thành **Mage** (Tăng MP, ATK Ma thuật).
- *Cặp dao găm mòn* -> Trở thành **Rogue** (Tăng Tốc độ, Tỷ lệ chí mạng).

### Step 2: Tương tác đầu tiên
Sau khi chọn vũ khí, AI tạo ra một chướng ngại vật nhỏ: Một cánh cửa đá bị kẹt hoặc một rương kho báu cũ. 
- *Mục tiêu:* Ép người chơi phải đưa ra hành động văn bản để giải quyết (ví dụ: dùng sức cạy cửa, dùng phép thiêu rụi chướng ngại vật).

### Step 3: Trận chiến đầu tiên (The Void Remnant)
Khi người chơi vừa bước ra khỏi Sảnh Khởi Nguyên, một sinh vật bóng tối nhỏ (Void Remnant) sẽ lao ra tấn công.
- **Quái vật:** Void Remnant (HP: 30 | ATK: 5)
- **AI Logic:** Hướng dẫn người chơi chọn lệnh "Tấn công" hoặc "Phòng thủ". Trận chiến này được thiết kế để người chơi **chắc chắn thắng**, nhằm giúp hệ thống Backend kiểm tra hàm tính toán sát thương (Combat Loop).

## Transition to Chapter 1
Sau khi tiêu diệt Void Remnant, quái vật tan biến thành tro bụi, để lại một viên đá phát sáng nhỏ (**Shard Tracker** - Vật phẩm cốt truyện). Giọng nói trong đầu lại vang lên: *"Màn sương đã phát hiện ra ngươi. Hãy chạy vào Rừng Thì Thầm..."*

Người chơi chính thức bước vào **Chapter 1: The Whispering Woods**.