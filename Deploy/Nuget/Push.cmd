::
@echo off
timeout /T 5

:: settings
set nugetPath=%cd%\..\.nuget

echo.
echo Pushing packages:
for /r %%f in (*.nupkg) do (
	echo   %%f
	"%nugetPath%\nuget.exe" push "%%f"
)

echo.
pause