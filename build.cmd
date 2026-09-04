@echo off
setlocal enabledelayedexpansion

echo =========================================================
echo   ORD Tarif Manager - Native Windows .NET Build System
echo =========================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC%" (
    echo [ERROR] Native .NET Framework csc.exe compiler not found!
    exit /b 1
)

set WPF_LIB=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF
if not exist "%WPF_LIB%" (
    set WPF_LIB=C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF
)

echo [INFO] Using compiler: %CSC%
echo [INFO] Using WPF libs: %WPF_LIB%

rem Gather all C# source files
set SOURCES=src\App.cs src\Core\*.cs src\UI\*.cs src\UI\Dialogs\*.cs

rem Embedded resources
set RESOURCES=/resource:src\UI\Styles\Theme.xaml,Theme.xaml ^
/resource:src\UI\Views\MainWindow.xaml,MainWindow.xaml ^
/resource:src\UI\Dialogs\BulkUpdateDialog.xaml,BulkUpdateDialog.xaml ^
/resource:src\UI\Dialogs\FilterDialog.xaml,FilterDialog.xaml ^
/resource:src\UI\Dialogs\CreateTariffDialog.xaml,CreateTariffDialog.xaml ^
/resource:src\UI\Dialogs\MatrixImportDialog.xaml,MatrixImportDialog.xaml ^
/resource:src\Resources\app.ico,app.ico ^
/resource:src\Resources\logo.png,logo.png

rem Assemblies
set REFS=/r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll,System.Xml.dll,System.Xml.Linq.dll,System.Data.dll,System.dll,System.Core.dll

echo [INFO] Compiling ORD_Tarif_Manager.exe...
"%CSC%" /nologo /target:winexe /win32icon:"src\Resources\app.ico" /optimize+ /codepage:65001 /out:"ORD_Tarif_Manager.exe" /lib:"%WPF_LIB%" %REFS% %RESOURCES% %SOURCES%

if %errorlevel% equ 0 (
    echo [SUCCESS] Build succeeded: ORD_Tarif_Manager.exe
    exit /b 0
) else (
    echo [ERROR] Build failed with exit code %errorlevel%
    exit /b 1
)
