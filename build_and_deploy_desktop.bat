@echo off
setlocal
chcp 65001 > nul
cd /d "%~dp0"

echo ========================================================
echo   놀티쳐 (KnolTeacher) 빌드 및 바탕화면 자동 배포
echo ========================================================
echo.

rem CI와 같은 공식 빌드 스크립트로 dist-net\놀티쳐.exe 한 파일을 만듭니다.
call publish.bat
if errorlevel 1 (
    echo.
    echo [배포 중단] 빌드에 실패했습니다. 위 오류 메시지를 확인해 주세요.
    pause
    exit /b 1
)

rem 현재 사용자의 바탕화면 경로를 사용합니다. OneDrive로 옮겨진 바탕화면도 지원합니다.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$desktop = [Environment]::GetFolderPath('Desktop'); $target = Join-Path $desktop '놀티쳐.exe'; Copy-Item -LiteralPath 'dist-net\놀티쳐.exe' -Destination $target -Force; Write-Host ('바탕화면에 복사했습니다: ' + $target)"
if errorlevel 1 (
    echo.
    echo [배포 실패] 바탕화면으로 복사하지 못했습니다. 놀티쳐가 실행 중이면 종료한 뒤 다시 시도해 주세요.
    pause
    exit /b 1
)

echo.
echo [완료] 바탕화면의 놀티쳐.exe를 실행하면 됩니다.
pause
exit /b 0
