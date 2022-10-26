@echo off
echo Disabling User Account Control

for /f "skip=2 tokens=2*" %%A ^
in ( 'reg.exe query HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System /v EnableLUA' ) ^
DO set LUA=%%~B

if %LUA% == 0x0 ( echo UAC is already disabled & exit )

reg.exe add HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System /v EnableLUA /t REG_DWORD /d 0 /f