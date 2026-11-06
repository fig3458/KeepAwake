@echo off
chcp 65001 >nul
cd /d "%~dp0"

REM 本机代理（Clash 等），Git 直连 GitHub 常会失败
set "HTTP_PROXY=http://127.0.0.1:7897"
set "HTTPS_PROXY=http://127.0.0.1:7897"
set "GIT=C:\Program Files\Git\cmd\git.exe"

"%GIT%" config --global http.https://github.com.proxy http://127.0.0.1:7897
"%GIT%" config --global https.https://github.com.proxy http://127.0.0.1:7897

echo === 推送 main ===
"%GIT%" push --force origin main
if errorlevel 1 (
  echo 推送失败。请确认代理 127.0.0.1:7897 已开启。
  pause
  exit /b 1
)

echo.
echo === 打开 Release 页，请上传 dist\KeepAwake.exe ===
start "" "https://github.com/fig3458/KeepAwake/releases/new?tag=v1.0.0"
explorer "%~dp0dist"
echo Tag: v1.0.0
echo 把 KeepAwake.exe 拖进网页 Attach binaries，再 Publish release
pause
