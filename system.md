# Loa Thành Kỳ Khí - Core System Documentation

Dự án sử dụng kiến trúc **Dual-Engine (Unity + Pygame)** để tái hiện huyền thoại nỏ thần An Dương Vương.

## ⚙️ 1. Technical Flow (Quy trình 4 Bước)

### Bước 1: Exploration (Unity)
- **Hành động**: Người chơi điều khiển nhân vật 3D thu thập tài nguyên (`copperCount`, `tinCount`).
- **Dữ liệu**: Lưu trữ cục bộ tại Unity `Inventory`.

### Bước 2 & 3: Refining & Assembly (Pygame)
- **Hành động**: Khi nhấn nâng cấp, Unity gửi lệnh `START_UPGRADE_PROCESS` sang Python.
- **Xử lý (Python)**:
    - **Refining**: Game giữ nhiệt độ lò (700-900°C) để đạt độ tinh khiết 100%.
    - **Assembly**: Giải đố lắp ráp bánh răng lẫy nỏ.
- **Kết quả**: Sau khi thắng cả 2, Python gửi lệnh `REQUIRE_CONSECRATION` về lại Unity.

### Bước 4: Consecration (Unity)
- **Hành động**: Unity kích hoạt chế độ "Thanh tẩy" (Chiến đấu hoặc thử thách thời gian).
- **Kết quả**: Hoàn tất thử thách, Unity cập nhật `SkinManager` để hiển thị Model mới.

## 🛡️ 2. Hệ thống Evolution Tiers
| Cấp độ | Tên Skin | Đặc điểm (Unity) |
| :--- | :--- | :--- |
| Tier 1 | Giáp Chàm | Vải, da thú thô sơ |
| Tier 2 | Giáp Đồng | Đồng xanh, hoa văn Đông Sơn |
| Tier 3 | Giáp Linh Quy | Hiệu ứng lân tinh mai rùa |
| Tier 4 | Thần Vương | Hào quang xoắn ốc vàng rực |

## 📡 3. Communication Schema
- **Unity -> Python**: `{ "action": "START_UPGRADE_PROCESS", "target_tier": 2 }`
- **Python -> Unity**: `{ "action": "REQUIRE_CONSECRATION", "next_tier": 2, "message": "..." }`

---
*Bản quyền ý tưởng thuộc về dự án Loa Thành Kỳ Khí.*
