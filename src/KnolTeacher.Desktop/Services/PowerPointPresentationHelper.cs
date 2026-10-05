using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace KnolTeacher.Desktop.Services;

/// <summary>
/// 파워포인트(PPT) 프레젠테이션을 교탁 PC(모니터 1) 작업과 무관하게
/// 모니터 2(전자칠판)에서 소리/영상 끊김 없이 지속 재생되도록 창 모드(ppShowTypeWindow)로 제어하는 헬퍼.
/// </summary>
public static class PowerPointPresentationHelper
{
    public const int PpShowTypeWindow = 2; // 웹 형식 창 모드 (포커스 상실 시에도 사운드/동영상 백그라운드 재생 유지)

    /// <summary>
    /// 로컬 시스템에 Microsoft PowerPoint 데스크톱 앱이 설치되어 있는지 확인합니다.
    /// </summary>
    public static bool IsPowerPointInstalled()
    {
        try
        {
            var pptType = Type.GetTypeFromProgID("PowerPoint.Application");
            return pptType != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 모니터 2(전자칠판)에 맞추어 사운드/영상 일시정지가 없는 창 모드 슬라이드 쇼를 실행합니다.
    /// </summary>
    /// <param name="pptFilePath">열고자 하는 PPT / PPTX 파일 전체 경로</param>
    /// <param name="targetBounds">배치할 모니터 화면 좌표 및 크기 (null이면 기본 슬라이드 쇼 실행)</param>
    public static (bool Success, string Message) StartNonIntrusiveSlideShow(string pptFilePath, Rect? targetBounds = null)
    {
        if (string.IsNullOrWhiteSpace(pptFilePath))
        {
            return (false, "파워포인트 파일 경로가 지정되지 않았습니다.");
        }

        if (!File.Exists(pptFilePath))
        {
            return (false, $"파워포인트 파일을 찾을 수 없습니다:\n{pptFilePath}");
        }

        string ext = Path.GetExtension(pptFilePath).ToLowerInvariant();
        if (ext != ".ppt" && ext != ".pptx" && ext != ".pps" && ext != ".ppsx")
        {
            return (false, "지원되지 않는 파일 형식입니다. (.ppt, .pptx, .pps, .ppsx만 지원)");
        }

        var pptType = Type.GetTypeFromProgID("PowerPoint.Application");
        if (pptType == null)
        {
            // 파워포인트 미설치 시 기본 연결 프로그램으로 안전하게 실행 폴백
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pptFilePath,
                    UseShellExecute = true
                });
                return (true, "Microsoft PowerPoint가 감지되지 않아 기본 연결 프로그램으로 열었습니다.");
            }
            catch (Exception ex)
            {
                return (false, $"파일을 열 수 없습니다: {ex.Message}");
            }
        }

        try
        {
            dynamic pptApp = Activator.CreateInstance(pptType)!;
            // PowerPoint 창을 보이게 설정 (msoTrue = -1 또는 1)
            try
            {
                pptApp.Visible = 1;
            }
            catch
            {
                // Visible 설정이 제한된 환경 예외 무시
            }

            dynamic presentations = pptApp.Presentations;
            // Presentations.Open(FileName, ReadOnly: 1, Untitled: 0, WithWindow: 1)
            dynamic presentation = presentations.Open(pptFilePath, 1, 0, 1);

            dynamic slideShowSettings = presentation.SlideShowSettings;
            // ppShowTypeWindow = 2 : 웹 형식으로 진행 (창 모드). 
            // 이 설정을 적용하면 다른 창이나 모니터 1을 클릭해도 음악과 비디오가 중단되지 않고 끊김 없이 유지됩니다.
            slideShowSettings.ShowType = PpShowTypeWindow;

            dynamic? slideShowWindow = slideShowSettings.Run();

            if (targetBounds is { } rect && slideShowWindow is not null)
            {
                try
                {
                    slideShowWindow.Left = (float)rect.Left;
                    slideShowWindow.Top = (float)rect.Top;
                    slideShowWindow.Width = (float)rect.Width;
                    slideShowWindow.Height = (float)rect.Height;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PowerPointPresentationHelper] 창 위치 조정 실패: {ex.Message}");
                }
            }

            return (true, "무간섭 파워포인트 슬라이드 쇼가 실행되었습니다.\n교탁 PC에서 다른 작업을 해도 소리와 영상이 계속 재생됩니다.");
        }
        catch (Exception ex)
        {
            // COM 예외 발생 시 기본 연결 프로그램 실행으로 폴백
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pptFilePath,
                    UseShellExecute = true
                });
                return (true, $"파워포인트 자동화 오류({ex.Message})로 인해 일반 실행 모드로 열었습니다.");
            }
            catch
            {
                return (false, $"파워포인트를 실행할 수 없습니다: {ex.Message}");
            }
        }
    }
}
