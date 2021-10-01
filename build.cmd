@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug
set MSBuildTarget=Build
set NodeReuse=true
set DeveloperCommandPrompt=%VS160COMNTOOLS%\VsDevCmd.bat
set MSBuildAdditionalArguments=/m
set RunTests=true
set DeployVsixExtension=true
set Solution=%Root%\ProjectFileTools.sln
set OnlyRestore=false
set SignType=public
set OfficialBuild=false
set VSTestAdditionalArguments=

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/rebuild" set MSBuildTarget=Rebuild&&shift&& goto :ParseArguments
if /I "%1" == "/restore" set OnlyRestore=true&&shift&& goto :ParseArguments
if /I "%1" == "/skiptests" set RunTests=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-deploy-extension" set DeployVsixExtension=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-node-reuse" set NodeReuse=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-multi-proc" set MSBuildAdditionalArguments=&&shift&& goto :ParseArguments
if /I "%1" == "/official-build" set SignType=real&&set OfficialBuild=true&&shift&& goto :ParseArguments
if /I "%1" == "/trx-test-results" set VSTestAdditionalArguments=/logger:trx&&shift&&goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if not exist "%VS160COMNTOOLS%" (
  echo To build this repository, this script needs to be run from a Visual Studio 2019 developer command prompt.
  echo.
  echo If Visual Studio is not installed, visit this page to download:
  echo.
  echo https://www.visualstudio.com/
  exit /b 1
)

if not exist "%VSSDKInstall%" (
  echo To build this repository, you need to modify your Visual Studio installation to include the "Visual Studio extension development" workload.
  exit /b 1
)

if "%VisualStudioVersion%" == "" (
  REM In Jenkins and MicroBuild, we set VS160COMNTOOLS and VSSDKInstall to point to where VS is installed but don't launch in a developer prompt
  call "%DeveloperCommandPrompt%" || goto :BuildFailed
)

set BinariesDirectory=%Root%bin\%BuildConfiguration%\
set RestoreLogFile=%BinariesDirectory%Restore.log
set LogFile=%BinariesDirectory%Build.log
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

echo.
echo Restoring packages
msbuild /nologo /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%RestoreLogFile%";verbosity=diagnostic /t:Restore /p:Configuration="%BuildConfiguration%" %Solution% %MSBuildAdditionalArguments%
if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Restore failed, for full log see %RestoreLogFile%."
    exit /b 1
)

echo.
call :PrintColor Green "Restore completed successfully, for full log see %RestoreLogFile%"

if "%OnlyRestore%" == "true" (
    exit /b 0
)

echo.
echo Building solution
msbuild /nologo /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%LogFile%";verbosity=diagnostic /t:"%MSBuildTarget%" /p:Configuration="%BuildConfiguration%" /p:DeployVsixExtension="%DeployVsixExtension%" /p:SignType="%SignType%" %Solution% %MSBuildAdditionalArguments%
if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Build failed, for full log see %LogFile%."
    exit /b 1
)

echo.
call :PrintColor Green "Build completed successfully, for full log see %LogFile%"

if "%RunTests%" == "false" (
    exit /b 0
)

echo.
echo Running tests
VSTest.Console.exe "bin\%BuildConfiguration%\ProjectFileTools.NuGetSearch.Tests.dll" %VSTestAdditionalArguments%
if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Tests failed, see console output for failed tests"
    exit /b 1
)

exit /b 0

:Usage
echo Usage: %BatchFile% [/rebuild^|/restore^|/modernvsixonly] [/debug^|/release] [/no-node-reuse] [/no-multi-proc] [/skiptests] [/no-deploy-extension]
echo.
echo   Build targets:
echo     /rebuild                 Perform a clean, then build
echo     /restore                 Only restore NuGet packages
echo     /skiptests               Don't run unit tests
echo.
echo   Build options:
echo     /debug                   Perform debug build (default)
echo     /release                 Perform release build
echo     /no-node-reuse           Prevents MSBuild from reusing existing MSBuild instances,
echo                              useful for avoiding unexpected behavior on build machines
echo     /no-multi-proc           No multi-proc build, useful for diagnosing build logs
echo     /no-deploy-extension     Does not deploy the VSIX extension when building the solution
echo     /official-build          Run the full signed build.
echo     /trx-test-results        Outputs MSTest logs in TRX format for test result archival
goto :eof

:BuildFailed
call :PrintColor Red "Build failed with ERRORLEVEL %ERRORLEVEL%"
exit /b 1

:PrintColor
"%Windir%\System32\WindowsPowerShell\v1.0\Powershell.exe" write-host -foregroundcolor %1 "'%2'"