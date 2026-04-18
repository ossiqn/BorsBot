@echo off
title BorsaBot Build Script v2.0
color 0A

echo.
echo  ==========================================
echo   BorsaBot Build Pipeline v2.0
echo  ==========================================
echo.

echo [1/6] .NET publish basliyor...
dotnet publish CSharp/BorsaBot/BorsaBot.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o publish
if %errorlevel% neq 0 ( echo HATA: .NET publish basarisiz & pause & exit )
echo [OK] .NET publish tamamlandi.

echo.
echo [2/6] Python dosyalari kopyalaniyor...
xcopy /E /I /Y Python Python_Build
if %errorlevel% neq 0 ( echo HATA: Kopyalama basarisiz & pause & exit )
echo [OK] Python dosyalari hazir.

echo.
echo [3/6] Python bagimliliklari kontrol ediliyor...
python --version > nul 2>&1
if %errorlevel% neq 0 ( echo HATA: Python bulunamadi & pause & exit )
echo [OK] Python mevcut.

echo.
echo [4/6] Requirements kontrol ediliyor...
pip install -r Python/requirements.txt --quiet
if %errorlevel% neq 0 ( echo HATA: pip kurulum basarisiz & pause & exit )
echo [OK] Bagimlilıklar hazir.

echo.
echo [5/6] Inno Setup ile installer olusturuluyor...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer\setup.iss
if %errorlevel% neq 0 ( echo HATA: Installer olusturulamadi & pause & exit )
echo [OK] Installer olusturuldu.

echo.
echo [6/6] Temizlik yapiliyor...
rmdir /S /Q Python_Build
rmdir /S /Q publish\__pycache__
echo [OK] Temizlik tamamlandi.

echo.
echo  ==========================================
echo   BUILD TAMAMLANDI -> dist\BorsaBot_Setup_v2.exe
echo  ==========================================
echo.
pause