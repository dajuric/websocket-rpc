::
@echo off
set nugetPath=%cd%\..\.nuget

:: Make sure the nuget executable is writable
attrib -R "%nugetPath%\nuget.exe"

echo.
echo Updating NuGet...
"%nugetPath%\nuget.exe" update -Self

echo.
pause