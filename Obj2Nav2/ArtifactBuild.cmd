@echo off
pushd "%~dp0"

powershell Compress-7Zip "bin\x64\Release" -ArchiveFileName "Obj2Nav2X64.zip" -Format Zip

:exit
popd
@echo on
