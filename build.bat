@echo off
dotnet clean
dotnet build --configuration Release
@dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
pause