@echo off
echo === Flash Hook 注入工具 ===
echo.

REM 找到游戏进程
for /f "tokens=2" %%a in ('tasklist /fi "imagename eq DDTankLauncher.exe" ^| findstr DDTankLauncher') do set PID=%%a
echo 游戏 PID: %PID%

REM 使用 rundll32 加载 DLL
echo 注入中...
rundll32.exe FlashHook.dll,Initialize
echo.
echo 请在游戏中调整角度，观察 hook_log.txt
