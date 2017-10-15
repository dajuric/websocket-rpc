::
@echo off
:: timeout /T 5

:: settings
set nugetPath=%cd%\..\..\.nuget
set version=1.0.2
set output=%cd%\bin

:: Create output directory
IF NOT EXIST "%output%\" (
    mkdir "%output%"
)

:: Remove old files
echo.
echo Remvoing old packages:
for /r %%f in (*.nupkg) do (
	echo   %%f
	del "%%f"
)

echo.
echo Creating packages for:
for /r %%f in (*.nuspec) do (
	echo   %%f
	"%nugetPath%\nuget.exe" pack "%%f" -Version %version% -OutputDirectory "%output%" -Verbosity quiet
)

echo.
pause