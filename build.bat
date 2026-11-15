@echo off
echo ===================================================
echo 1. Building Laptop Input Shield ^& Controller...
echo ===================================================

"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" LaptopInputControl.csproj /p:Configuration=Release /p:Platform=x64
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Application build failed!
    pause
    exit /b %ERRORLEVEL%
)

copy /Y "bin\Release\LaptopInputControl.exe" "LaptopInputControl.exe" >nul
copy /Y "bin\Release\LaptopInputControl.exe" "Installer\Payload\LaptopInputControl.exe" >nul

echo.
echo ===================================================
echo 2. Building Uninstaller...
echo ===================================================
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:"Installer\Payload\Uninstall.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "Installer\Uninstaller.cs"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Uninstaller build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================
echo 3. Building Standalone Setup.exe Installer...
echo ===================================================
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "Installer\Setup.csproj" /p:Configuration=Release /p:Platform=x64
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Setup build failed!
    pause
    exit /b %ERRORLEVEL%
)

copy /Y "Installer\bin\Release\LaptopInputControl_Setup.exe" "LaptopInputControl_Setup.exe" >nul
copy /Y "Installer\bin\Release\LaptopInputControl_Setup.exe" "Setup.exe" >nul

echo.
echo ===================================================
echo [SUCCESS] All binaries and Installers built!
echo - Portable App: %~dp0LaptopInputControl.exe
echo - Setup Wizard: %~dp0Setup.exe
echo ===================================================
echo.
pause
