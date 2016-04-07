@echo off
SET "MSBuild40=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
@echo on

REM <===========================================================================>
REM <=== I. Clean up Temporary Build Folders (after previous build)
REM <===========================================================================>

rd /S /Q "%InstallerTemp%"
del /Q /F *.*

REM <===========================================================================>
REM <=== II. Generate BINARIES
REM <===========================================================================>

REM <-- 1. Console Application -->

REM <-- build SensorTool -->
%MSBuild40% "%RepPath%\SensorTool\SensorTool.sln" /t:Clean;Rebuild /p:Configuration=Release /p:Platform="Any CPU"
IF %ERRORLEVEL% NEQ 0 EXIT 1


REM <-- 1. Delete all trash files from Release folder -->
del /Q /F "%RepPath%\SensorTool\SensorTool\bin\Release\*.pdb"

REM <-- copy Database & WiX files -->
xcopy "%RepPath%\SensorTool\SensorTool\bin\Release" "%InstallerTemp%\Release\" /Q /Y /E
xcopy "%RepPath%\Documentation\*.*" "%InstallerTemp%\Release\Documentation\" /Q /Y /E
xcopy "%RepPath%\BuildScripts\WiX" "%InstallerTemp%\" /Q /Y /E

REM <-- generate MSI -->
"%WIX%\bin\heat" dir "%InstallerTemp%\Release" -dr INSTALLFOLDER -cg cmpG_SensorTool_Main -sreg -scom -gg -g1 -sf -srd -var "var.MyDir" -out "%InstallerTemp%\SensorTool_Manifest.wxs"

"%WIX%\bin\candle" -dMyDir=Release "%InstallerTemp%\*.wxs" -out "%InstallerTemp%\\"  -ext "WixNetFxExtension.dll" -ext "WixUIExtension.dll"
IF %ERRORLEVEL% NEQ 0 EXIT 1
"%WIX%\bin\light" -out "iTK_SensorTool.%MainVersionNumber%.%BUILD_NUMBER%.msi" -ext "WixNetFxExtension.dll" -ext "WixUIExtension.dll" -sice:ICE57 "%InstallerTemp%\*.wixobj"
IF %ERRORLEVEL% NEQ 0 EXIT 1

REM <===========================================================================>
REM <=== III. Archive the artifacts
REM <===========================================================================>

REM <-- 1. Delete all trash files -->
del /Q /F *.wixpdb