@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Запуск локального сервера для AR-страницы...
echo Откройте в браузере: http://localhost:8080/
powershell -NoProfile -ExecutionPolicy Bypass -File server.ps1
pause
