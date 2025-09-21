@echo off
:: 自动生成项目目录树到 project_structure.txt
:: /F = 列出文件
:: /A = 使用 ASCII 字符(方便复制)
tree /F /A > project_structure.txt
echo ---------------------------------------
echo 导出完成！文件已生成：project_structure.txt
echo 按任意键退出...
pause >nul
