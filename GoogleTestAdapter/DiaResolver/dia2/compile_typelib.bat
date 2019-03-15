@echo off
if %VisualStudioVersion%. == . call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat" >nul
powershell -executionpolicy bypass -file compile_typelib.ps1
