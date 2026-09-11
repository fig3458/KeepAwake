@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [错误] 未找到 csc.exe，请确认已安装 .NET Framework 4.x
  exit /b 1
)

echo 使用编译器: %CSC%
if not exist bin mkdir bin
if not exist dist mkdir dist

echo 编译 KeepAwake 单文件版（管理员清单）...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
  /win32manifest:app.manifest ^
  /out:bin\KeepAwake.exe ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  Program.cs MainForm.cs PowerTweaks.cs

if errorlevel 1 (
  echo 编译失败
  exit /b 1
)

copy /Y app.config bin\KeepAwake.exe.config >nul
copy /Y bin\KeepAwake.exe dist\KeepAwake.exe >nul
copy /Y app.config dist\KeepAwake.exe.config >nul

echo.
echo ========================================
echo  输出: dist\KeepAwake.exe
echo  双击会弹出 UAC（管理员）
echo ========================================
exit /b 0
