# **TÀI LIỆU Ý TƯỞNG DỰ ÁN GAME**

# **LOA THÀNH KỲ KHÍ**

## **I. TỔNG QUAN DỰ ÁN**

**Loa Thành Kỳ Khí** là một trò chơi thuộc thể loại **Action-RPG 3D** kết hợp yếu tố **Adventure, Simulation, Puzzle và Crafting**. Trò chơi lấy bối cảnh thời kỳ **An Dương Vương xây dựng thành Cổ Loa**, tập trung vào hành trình khám phá, thu thập nguyên liệu, giải mã di tích cổ và phục dựng các món **Kỳ Khí** nhằm bảo vệ đất nước Âu Lạc khỏi thế lực tà ác.

Người chơi sẽ hóa thân thành một hậu duệ của dòng họ nghệ nhân chế tác binh khí, mang nhiệm vụ tìm lại những tri thức thất truyền về luyện kim, cơ khí và sức mạnh thần linh để tạo ra các vũ khí, giáp trụ huyền thoại như **Giáp Linh Quy**, **Nỏ Thần** và **Thần Vương Giáp**.

Dự án được phát triển hoàn toàn bằng **Unity 3D**, sử dụng **C#** để xử lý toàn bộ gameplay, hệ thống chiến đấu, nâng cấp trang bị, giải đố, lưu dữ liệu và quản lý tiến trình người chơi.

### **Điểm đặc trưng**

**Unity 3D Gameplay:** Xây dựng thế giới 3D, nhân vật, môi trường, hệ thống chiến đấu, hoạt ảnh và tương tác vật lý.

**Action-RPG:** Người chơi điều khiển nhân vật chiến đấu thời gian thực, né tránh, tấn công, sử dụng kỹ năng và nâng cấp sức mạnh.

**Crafting & Upgrade System:** Thu thập nguyên liệu, tinh luyện vật liệu, chế tạo và nâng cấp giáp trụ, vũ khí, Kỳ Khí.

**Puzzle System:** Giải các câu đố cơ khí cổ đại dựa trên bánh răng, ký hiệu Trống Đồng và cấu trúc xoắn ốc của Cổ Loa.

**Văn hóa lịch sử Việt Nam:** Khai thác các yếu tố Trống Đồng Đông Sơn, kiến trúc xoắn ốc Cổ Loa, Thần Kim Quy và truyền thuyết Nỏ Thần.

## **II. KIẾN TRÚC KỸ THUẬT**

Hệ thống game được xây dựng theo mô hình **Unity 3D Single-Engine Architecture**, trong đó toàn bộ chức năng của trò chơi được xử lý bên trong Unity.

### **1\. Unity 3D - Game Engine chính**

Unity 3D đóng vai trò là nền tảng phát triển chính của dự án, đảm nhiệm:

Hiển thị thế giới 3D.

Điều khiển nhân vật góc nhìn thứ ba.

Xử lý va chạm và vật lý.

Hệ thống chiến đấu thời gian thực.

Hệ thống nhiệm vụ.

Hệ thống chế tạo và nâng cấp.

Hệ thống giải đố.

Hệ thống AI của quái vật.

Hiệu ứng hình ảnh, âm thanh và hoạt ảnh.

Lưu và tải dữ liệu người chơi.

### **2\. C# - Xử lý logic gameplay**

C# được sử dụng để lập trình toàn bộ logic trong game, bao gồm:

Player Controller.

Combat System.

Enemy AI.

Inventory System.

Crafting System.

Equipment System.

Upgrade System.

Quest System.

Puzzle System.

Save/Load System.

UI System.

### **3\. Cơ sở dữ liệu / Lưu trữ dữ liệu**

Dữ liệu người chơi có thể được lưu bằng một trong các hướng sau:

**PlayerPrefs:** Lưu dữ liệu đơn giản như âm lượng, cấu hình, tiến trình nhỏ.

**JSON File:** Lưu thông tin nhân vật, inventory, nhiệm vụ, trang bị và cấp độ nâng cấp.

**MySQL / SQLite:** Dùng nếu dự án cần quản lý tài khoản, bảng xếp hạng hoặc dữ liệu phức tạp hơn.

Đối với phiên bản đồ án, nên ưu tiên sử dụng **JSON File hoặc SQLite** để dễ triển khai.

## **III. HỆ THỐNG NÂNG CẤP**

Trò chơi xây dựng hệ thống phát triển nhân vật xoay quanh việc phục dựng và nâng cấp **Skin Giáp Trụ** cùng **Nỏ Thần**.

### **1\. Phân cấp Skin**

| **Cấp độ** | **Tên Skin**  | **Đặc điểm đồ họa trong Unity 3D**         | **Ý nghĩa văn hóa**           |
| ---------- | ------------- | ------------------------------------------ | ----------------------------- |
| Tier 1     | Giáp Chàm     | Chất liệu vải, da thú thô sơ, màu sắc trầm | Thời kỳ đầu dựng nước         |
| Tier 2     | Giáp Đồng     | Kim loại đồng xanh, hoa văn Trống Đồng     | Kỷ nguyên đồ đồng Đông Sơn    |
| Tier 3     | Giáp Linh Quy | Hiệu ứng phát sáng xanh, họa tiết mai rùa  | Sự giúp đỡ của Thần Kim Quy   |
| Tier 4     | Thần Vương    | Hào quang xoắn ốc, giáp trụ vàng rực rỡ    | Đỉnh cao quyền lực và kỹ nghệ |

### **2\. Quy trình nâng cấp**

Trò chơi sử dụng vòng lặp nâng cấp gồm 4 bước chính:

#### **Bước 1: Exploration - Khám phá**

Người chơi điều khiển nhân vật trong môi trường 3D để:

Khám phá rừng, hang động, phế tích và khu vực quanh Cổ Loa.

Thu thập khoáng sản đồng, thiếc, đá cổ và mảnh di vật.

Chiến đấu với quái vật bị Hắc Khí tha hóa.

Mở khóa khu vực mới và nhiệm vụ mới.

#### **Bước 2: Refining - Tinh luyện**

Người chơi sử dụng hệ thống chế tác trong Unity để tinh luyện nguyên liệu.

Cơ chế có thể bao gồm:

Chọn loại nguyên liệu.

Điều chỉnh tỉ lệ đồng / thiếc.

Canh nhiệt độ luyện kim.

Hoàn thành mini-game canh thời điểm.

Nhận kết quả vật liệu theo độ hiếm: Thường, Tốt, Tinh Xảo, Huyền Khí.

#### **Bước 3: Assembly - Lắp ráp**

Người chơi giải đố cơ khí để lắp ráp Kỳ Khí.

Các dạng puzzle có thể gồm:

Xoay bánh răng đúng hướng.

Sắp xếp ký hiệu Trống Đồng.

Kết nối luồng năng lượng trên bảng cơ khí.

Mở khóa cấu trúc lẫy nỏ.

Ghép các mảnh giáp theo đúng vị trí.

#### **Bước 4: Consecration - Khai Quang**

Sau khi chế tạo thành công, người chơi phải hoàn thành thử thách cuối trong Unity 3D để kích hoạt sức mạnh của Kỳ Khí.

Thử thách có thể bao gồm:

Đánh bại một nhóm quái vật.

Bảo vệ lò luyện trong thời gian giới hạn.

Vượt qua thử thách của Thần Kim Quy.

Kích hoạt trận pháp xoắn ốc của Loa Thành.

Khi hoàn tất, game sẽ cập nhật model giáp mới, mở khóa chỉ số mới, hiệu ứng mới và kỹ năng chiến đấu mới.

## **VI. KẾT LUẬN**

Phiên bản **Loa Thành Kỳ Khí Unity 3D** tập trung vào việc xây dựng một trò chơi hành động nhập vai 3D có yếu tố văn hóa Việt Nam, kết hợp giữa chiến đấu, khám phá, chế tạo và giải đố.

Việc chuyển toàn bộ hệ thống sang Unity 3D giúp dự án dễ triển khai hơn, giảm độ phức tạp kỹ thuật, đồng thời phù hợp hơn với quy mô đồ án sinh viên. Toàn bộ gameplay, logic nâng cấp và trải nghiệm người chơi đều được xử lý thống nhất bằng Unity và C#, giúp quá trình phát triển, kiểm thử và trình bày dự án trở nên rõ ràng, thực tế hơ**ỐT TRUYỆN: LOA THÀNH KỲ KHÍ**

## **"Di Sản Cổ Loa"**

### **1\. Khởi Nguyên: Bí Ẩn Dưới Chân Thành**

Vào thời kỳ An Dương Vương xây dựng thành Cổ Loa, vùng đất Âu Lạc liên tục bị đe dọa bởi những thế lực bí ẩn xuất hiện từ lòng đất. Chúng được gọi là U Minh Tộc - những sinh vật bị tha hóa bởi nguồn năng lượng cổ xưa mang tên Hắc Khí.

Mỗi khi tường thành được dựng lên, Hắc Khí lại khiến chúng sụp đổ. Những thanh kiếm vừa được rèn xong nhanh chóng gỉ sét, còn binh sĩ dần mất đi ý chí chiến đấu.

Trong lúc đất nước đứng trước nguy cơ diệt vong, một truyền thuyết cổ được nhắc lại: chỉ những "Kỳ Khí" thất lạc của tổ tiên mới có thể chống lại nguồn sức mạnh tà ác này.

### **2\. Nhân Vật Chính: Cao Thục**

Người chơi vào vai Cao Thục - hậu duệ cuối cùng của dòng họ nghệ nhân chế tác binh khí nổi tiếng tại Âu Lạc.

Trong một lần khám phá khu vực nền móng cổ của Loa Thành, Cao Thục phát hiện một cổ vật mang tên:

**Thiên Cơ Bàn**

Đây là bảo vật được cho là do Thần Kim Quy để lại, chứa đựng tri thức về cơ khí, luyện kim và những bí mật thất truyền của người Đông Sơn.

Nhờ Thiên Cơ Bàn, Cao Thục có khả năng phân tích cấu trúc vật liệu, khám phá các cơ quan cổ đại và phục dựng những Kỳ Khí huyền thoại đã bị chôn vùi qua hàng thế kỷ.

### **3\. Hành Trình Trở Thành Kỳ Khí Sư**

#### **Hồi I: Giáp Chàm - Khởi Đầu Người Thợ**

Cao Thục bắt đầu hành trình chỉ với bộ Giáp Chàm đơn sơ.

Người chơi khám phá rừng núi, hang động và các phế tích quanh Cổ Loa để thu thập đồng thô, đá cổ và mảnh vỡ di vật.

Thông qua hệ thống chế tác, người chơi học cách tinh luyện vật liệu và chế tạo những trang bị đầu tiên nhằm chống lại các sinh vật bị Hắc Khí tha hóa.

#### **Hồi II: Giáp Đồng - Hồn Thiêng Đông Sơn**

Khi thế lực U Minh ngày càng mạnh hơn, Cao Thục phát hiện các bí mật được khắc trên những chiếc Trống Đồng cổ.

Các hoa văn Đông Sơn thực chất là sơ đồ vận hành của những cỗ máy cổ đại.

Người chơi phải giải các câu đố cơ khí, kích hoạt các công trình cổ và tìm kiếm những linh kiện thất lạc để nâng cấp trang bị lên Giáp Đồng.

Bộ giáp mới giúp nhân vật tăng sức phòng thủ và mở khóa nhiều kỹ năng chiến đấu hơn.

#### **Hồi III: Giáp Linh Quy - Di Sản Thần Kim Quy**

Trong quá trình xây dựng vòng thành thứ ba của Cổ Loa, những cuộc tấn công của U Minh Tộc trở nên dữ dội hơn bao giờ hết.

Thần Kim Quy xuất hiện trong giấc mơ và tiết lộ rằng nguồn sức mạnh bảo vệ Âu Lạc nằm sâu dưới Đầm Linh Quy.

Sau khi vượt qua các thử thách cổ xưa, Cao Thục tìm thấy Mảnh Giáp Thần - vật liệu thần bí mang năng lượng của Kim Quy.

Từ đó, người chơi chế tạo được Giáp Linh Quy, bộ giáp có khả năng kháng lại tà thuật và phản đòn năng lượng Hắc Khí.

#### **Hồi IV: Thần Vương - Trái Tim Của Loa Thành**

Khi U Minh Tộc mở cánh cổng cuối cùng và triệu hồi đại quân hắc ám, bí mật lớn nhất của Cổ Loa được hé lộ.

Toàn bộ Loa Thành thực chất là một công trình cơ khí khổng lồ được thiết kế để bảo vệ Âu Lạc.

Để kích hoạt sức mạnh tối thượng của thành, Cao Thục phải thu thập toàn bộ Kỳ Khí thất lạc và hợp nhất chúng với Thiên Cơ Bàn.

Sau khi hoàn thành nghi thức Khai Quang, bộ giáp Thần Vương thức tỉnh.

Người chơi sở hữu sức mạnh của cả kỹ nghệ Đông Sơn và ý chí của Thần Kim Quy, có thể điều khiển Nỏ Thần triệu hồi hàng ngàn mũi tên đồng để chống lại đạo quân U Minh.

### **4\. Kết Thúc: Di Sản Muôn Đời**

Sau trận chiến cuối cùng, U Minh Tộc bị phong ấn vĩnh viễn.

Loa Thành được hoàn thiện và trở thành biểu tượng cho trí tuệ, kỹ nghệ và tinh thần bất khuất của dân tộc Âu Lạc.

Cao Thục không trở thành một vị vua hay anh hùng huyền thoại.

Anh trở thành người gìn giữ tri thức của Kỳ Khí, truyền lại những bí mật chế tác cho các thế hệ sau.

Từ đó, truyền thuyết về những người Kỳ Khí Sư được lưu truyền mãi trong lịch sử.

# **CORE GAMEPLAY LOOP - LOA THÀNH KỲ KHÍ**

## **Mục tiêu người chơi**

Người chơi vào vai Cao Thục, thực hiện nhiệm vụ để bảo vệ vùng đất Cổ Loa, tiêu diệt quái vật Hắc Khí, thu thập nguyên liệu và chế tạo các bộ giáp, vũ khí ngày càng mạnh hơn.

# **CORE LOOP**

### **1\. Nhận nhiệm vụ**

Người chơi nhận nhiệm vụ từ NPC trong Thành Cổ Loa.

Ví dụ:

Tiêu diệt 10 U Minh Binh.

Thu thập 15 Đồng Thô.

Đánh bại Thủ Lĩnh Hang Động.

Tìm Mảnh Giáp Linh Quy.

Phần thưởng:

Kinh nghiệm (EXP)

Vàng

Nguyên liệu

Công thức chế tạo

↓

### **2\. Khám phá bản đồ**

Người chơi đi tới:

Rừng Cổ Loa

Hầm Mỏ Đồng

Đầm Linh Quy

Tàn Tích Đông Sơn

Tại đây người chơi:

Tìm rương báu

Thu thập nguyên liệu

Gặp quái vật

Hoàn thành nhiệm vụ

↓

### **3\. Chiến đấu**

Đối đầu với:

U Minh Binh

Sói Hắc Khí

Cự Nhân Đá

Boss Khu Vực

Người chơi:

Tấn công

Né tránh

Sử dụng kỹ năng

Nhận:

EXP

Vàng

Nguyên liệu rơi ra

↓

### **4\. Thu thập nguyên liệu**

Các loại nguyên liệu:

Đồng Thô

Thiếc

Đá Linh Khí

Mai Linh Quy

Hắc Tinh Thạch

Nguyên liệu được dùng để chế tạo trang bị.

↓

### **5\. Chế tạo và nâng cấp**

Tại Lò Rèn Cổ Loa:

Người chơi dùng nguyên liệu để:

Chế tạo giáp mới

Chế tạo vũ khí mới

Nâng cấp trang bị hiện có

Ví dụ:

Giáp Chàm

↓

Giáp Đồng

↓

Giáp Linh Quy

↓

Thần Vương

Trang bị càng mạnh:

Tăng HP

Tăng sát thương

Tăng phòng thủ

↓

### **6\. Mở khóa khu vực mới**

Khi đạt cấp độ hoặc hoàn thành nhiệm vụ chính:

Mở bản đồ mới

Gặp quái mạnh hơn

Thu thập nguyên liệu hiếm hơn

Chế tạo trang bị cấp cao hơn

↓

Quay lại bước 1