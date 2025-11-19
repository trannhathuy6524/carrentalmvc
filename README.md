1)Quy trình làm việc
Trước khi code
  git checkout main
  git pull origin main # lấy code mới nhất
  git checkout -b feature/ten-cua-ban
  
Khi code xong
  git add .
  git commit -m "feat: mô tả ngắn"
  git push origin feature/ten-cua-ban
  
Trên GitHub → tạo Pull Request từ feature/ten-cua-ban vào main.
  git checkout main
  git pull origin main
  git branch -d feature/ten-cua-ban # xoá local branch đã merge
  
2) Tạo Pull Request & review (chi tiết)
Trên GitHub → Pull requests → New pull request
Chọn base: main, compare: branch của bạn
Title: ghi ngắn gọn (VD Feat: Admin Dashboard) và mô tả chi tiết: hành vi mới, config cần thay đổi, migration DB (nếu có), ảnh nếu cần
Trong PR có thể assign reviewer (2 thành viên còn lại) — tốt nhất require 1 reviewer approve
Sau khi review OK → Merge vào main (chọn Merge commit hoặc Squash — team nên thống nhất)
