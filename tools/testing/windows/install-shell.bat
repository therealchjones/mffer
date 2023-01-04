curl -LSsO https://github.com/git-for-windows/git/releases/download/v2.38.1.windows.1/Git-2.38.1-64-bit.exe
Git-2.38.1-64-bit.exe /VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS
reg add HKLM\SOFTWARE\OpenSSH /v DefaultShell /d "C:\Program Files\Git\bin\bash.exe" /f