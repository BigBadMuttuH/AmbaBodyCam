@echo off
setlocal

:: Указываем путь к решению
set SOLUTION_NAME=AmbaBodyCam.sln
set OUTPUT_DIR=publish

:: Очистка и создание папки для публикации
echo Очистка старых сборок...
rd /s /q %OUTPUT_DIR%
mkdir %OUTPUT_DIR%

:: Очистка проекта
echo Очистка проекта...
dotnet clean %SOLUTION_NAME%

:: Сборка всех проектов в Release
echo Сборка проектов в Release...
dotnet build %SOLUTION_NAME% --configuration Release

:: Публикация AmbaSimpleClass (библиотека классов)
echo Публикация AmbaSimpleClass...
dotnet pack AmbaSimpleClass/AmbaSimpleClass.csproj -c Release -o %OUTPUT_DIR%\AmbaSimpleClass

:: Публикация AmbaConsoleApp (консольное приложение)
echo Публикация AmbaConsoleApp...
dotnet publish AmbaConsoleApp/AmbaConsoleApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o %OUTPUT_DIR%\AmbaConsoleApp

:: Публикация AmbaWpfApp (WPF-приложение)
echo Публикация AmbaWpfApp...
dotnet publish AmbaWpfApp/AmbaWpfApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o %OUTPUT_DIR%\AmbaWpfApp

echo Сборка завершена! Файлы находятся в папке %OUTPUT_DIR%
pause
