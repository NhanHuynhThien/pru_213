# HƯỚNG DẪN CÀI ĐẶT DỰ ÁN VỚI GIT LFS (LARGE FILE STORAGE)

Dự án này sử dụng **Git LFS** để quản lý các file lớn (như model 3D `.fbx`, `.glb` có dung lượng trên 100MB) nhằm vượt qua giới hạn dung lượng của GitHub.

Khi đẩy code (push) lên GitHub, dữ liệu thực tế của các file lớn này đã được tải lên máy chủ lưu trữ LFS của GitHub an toàn. Trên GitHub chỉ lưu các file text pointer rất nhỏ để tham chiếu.

Dưới đây là hướng dẫn chi tiết dành cho người mới khi tải (clone/pull) dự án này về để đảm bảo không bị lỗi mô hình hộp màu xanh (generic boxes) trong Unity.

---

## TRƯỜNG HỢP 1: TẢI (CLONE) DỰ ÁN MỚI HOÀN TOÀN

Nếu bạn là thành viên mới và chuẩn bị tải dự án về máy lần đầu, hãy làm theo các bước sau:

### Bước 1: Cài đặt Git LFS trên máy tính của bạn
* **Windows**: Nếu bạn cài đặt Git cho Windows bản mới, Git LFS thường đã có sẵn. Nếu chưa, hãy tải installer từ [git-lfs.github.com](https://git-lfs.github.com/) và chạy cài đặt.
* **macOS**: Cài đặt qua Homebrew bằng lệnh:
  ```bash
  brew install git-lfs
  ```

### Bước 2: Kích hoạt Git LFS trên máy tính (Chỉ cần làm 1 lần duy nhất)
Mở terminal (PowerShell, Command Prompt hoặc Git Bash) và chạy lệnh:
```bash
git lfs install
```
*(Nếu thấy thông báo `Git LFS initialized.` tức là đã thành công).*

### Bước 3: Clone dự án bình thường
Bây giờ, bạn tiến hành clone dự án về máy. Git LFS sẽ tự động nhận diện các file lớn và tải toàn bộ dữ liệu 3D thực tế về máy của bạn cùng lúc:
```bash
git clone https://github.com/DuongDinhKhoi-2607/PRU213.git
```
Sau khi clone xong, bạn có thể mở Unity lên chơi ngay mà không gặp bất kỳ lỗi nào!

---

## TRƯỜNG HỢP 2: ĐÃ CLONE DỰ ÁN NHƯNG BỊ LỖI HÌNH HỘP MÀU XANH / THIẾU FILE 3D

Nếu bạn đã clone dự án về trước đó mà chưa cài đặt Git LFS, các file mô hình 3D trong Unity sẽ bị lỗi màu đỏ hoặc hiện hình hộp màu xanh (do Git chỉ tải về file text pointer nhỏ). 

Để sửa lỗi này, hãy thực hiện các bước sau ngay tại thư mục dự án của bạn:

1. Mở terminal tại thư mục dự án (`PRU213`).
2. Chạy lệnh kích hoạt LFS:
   ```bash
   git lfs install
   ```
3. Chạy lệnh kéo (pull) toàn bộ dữ liệu file lớn thực tế về máy:
   ```bash
   git lfs pull
   ```
   *Lệnh này sẽ tự động tải các file `.fbx`, `.glb` thực tế và ghi đè lên các tệp pointer.*
4. Quay lại Unity, Unity sẽ tự động nhập lại (re-import) mô hình và mọi lỗi sẽ biến mất!

---

## MỘT SỐ LƯU Ý KHI LÀM VIỆC NHÓM
* **Không cần commit lại**: Khi bạn sửa code hoặc cập nhật mô hình, hãy cứ commit và push bình thường. Git LFS sẽ tự động xử lý tách biệt file code lên Git và file 3D lên LFS cho bạn.
* **Kiểm tra trạng thái**: Bạn có thể gõ lệnh `git lfs status` để xem các file lớn nào đang được quản lý.
