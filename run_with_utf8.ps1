# Скрипт для запуска StoriesLinker с корректной кодировкой UTF-8
# Устанавливаем кодировку консоли
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding = [System.Text.Encoding]::UTF8

# Устанавливаем кодировку PowerShell
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "Запуск StoriesLinker с поддержкой UTF-8..." -ForegroundColor Green

# Запускаем приложение
dotnet run --project StoriesLinker/StoriesLinker.csproj 