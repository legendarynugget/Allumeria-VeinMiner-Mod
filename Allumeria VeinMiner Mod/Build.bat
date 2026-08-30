@echo off
setlocal enabledelayedexpansion
title Vein Miner - Build Console

set "GAME_DIR=F:\SteamLibrary\steamapps\common\Allumeria"

if not exist "%GAME_DIR%" (
    if exist "C:\Program Files (x86)\Steam\steamapps\common\Allumeria" (
        set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Allumeria"
    ) else if exist "D:\SteamLibrary\steamapps\common\Allumeria" (
        set "GAME_DIR=D:\SteamLibrary\steamapps\common\Allumeria"
    )
)

set "MOD_DIR=%GAME_DIR%\mods\veinminer"

echo ==============================================================================
echo                      VEIN MINER - BUILD AND DEPLOYMENT
echo ==============================================================================
echo Current Folder:        "%CD%"
echo Target Game Folder:    "%GAME_DIR%"
echo Target Mod Folder:     "%MOD_DIR%"
echo ==============================================================================
echo.

echo [STEP 1/4] Checking .NET SDK...
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET SDK is not in PATH!
    goto :FAIL
)
echo.

echo [STEP 2/4] Restoring packages...
dotnet restore VeinMiner.csproj --verbosity minimal
if %ERRORLEVEL% NEQ 0 goto :FAIL
echo.

echo [STEP 3/4] Compiling Vein Miner...
dotnet build VeinMiner.csproj -c Release --no-restore
if %ERRORLEVEL% NEQ 0 goto :FAIL
echo.

echo [STEP 4/4] Deploying to Allumeria mods directory...
if not exist "%MOD_DIR%" mkdir "%MOD_DIR%"

set "SOURCE_DLL="
if exist "bin\Release\net10.0\VeinMiner.dll" set "SOURCE_DLL=bin\Release\net10.0\VeinMiner.dll"
if exist "bin\Release\net9.0\VeinMiner.dll"  set "SOURCE_DLL=bin\Release\net9.0\VeinMiner.dll"

if not defined SOURCE_DLL (
    echo [ERROR] Could not find VeinMiner.dll in bin\Release\
    goto :FAIL
)

copy /Y "%SOURCE_DLL%" "%MOD_DIR%\VeinMiner.dll" >nul
echo   - Installed: VeinMiner.dll

if exist "Metadata.json" (
    copy /Y "Metadata.json" "%MOD_DIR%\Metadata.json" >nul
    echo   - Installed: Metadata.json
) else (
    echo [ERROR] Metadata.json missing!
    goto :FAIL
)

echo.
echo ==============================================================================
echo [SUCCESS] Vein Miner built and installed successfully!
echo           Hold 'V' while mining an ore or tree to mine the entire vein.
echo ==============================================================================
goto :END

:FAIL
echo.
echo ==============================================================================
echo [FAILED] Build or deployment failed! Check the errors above.
echo ==============================================================================

:END
echo.
pause