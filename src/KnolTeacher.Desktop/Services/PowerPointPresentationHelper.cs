using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private static dynamic? _currentSlideShowWindow = null;

    /// <summary>
    /// 현재 열려 있는 프레젠테이션 파일명 (예: 과학_수업자료.pptx)
    /// </summary>
    public static string? CurrentPresentationTitle { get; private set; }

    /// <summary>
    /// 슬라이드 쇼 실행/종료 상태가 변경될 때 발생하는 이벤트.
    /// </summary>
    public static event Action<bool>? SlideShowStateChanged;

    /// <summary>
    /// 현재 무간섭 슬라이드 쇼가 활성화되어 있는지 여부.
    /// </summary>
    public static bool IsSlideShowRunning
    {
        get
        {
            if (_currentSlideShowWindow == null) return false;
            try
            {
                // COM 객체 생존 여부 확인
                var view = _currentSlideShowWindow.View;
                return view != null;
            }
            catch
            {
                _currentSlideShowWindow = null;
                CurrentPresentationTitle = null;
                SlideShowStateChanged?.Invoke(false);
                return false;
            }
        }
    }

    /// <summary>
    /// 교탁 PC에서 마우스 커서를 모니터 2로 옮기지 않고도 다음 슬라이드(또는 애니메이션)로 넘깁니다.
    /// </summary>
    public static bool NextSlide()
    {
        if (_currentSlideShowWindow == null) return false;
        try
        {
            _currentSlideShowWindow.View.Next();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerPointPresentationHelper] NextSlide 실패: {ex.Message}");
            _currentSlideShowWindow = null;
            CurrentPresentationTitle = null;
            SlideShowStateChanged?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 교탁 PC에서 마우스 커서를 모니터 2로 옮기지 않고도 이전 슬라이드로 되돌립니다.
    /// </summary>
    public static bool PreviousSlide()
    {
        if (_currentSlideShowWindow == null) return false;
        try
        {
            _currentSlideShowWindow.View.Previous();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerPointPresentationHelper] PreviousSlide 실패: {ex.Message}");
            _currentSlideShowWindow = null;
            CurrentPresentationTitle = null;
            SlideShowStateChanged?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 첫 번째 슬라이드로 바로 이동합니다.
    /// </summary>
    public static bool FirstSlide()
    {
        if (_currentSlideShowWindow == null) return false;
        try
        {
            _currentSlideShowWindow.View.First();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerPointPresentationHelper] FirstSlide 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 마지막 슬라이드로 바로 이동합니다.
    /// </summary>
    public static bool LastSlide()
    {
        if (_currentSlideShowWindow == null) return false;
        try
        {
            _currentSlideShowWindow.View.Last();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerPointPresentationHelper] LastSlide 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 화면을 일시적으로 검게 가리거나(Blackout) 원래대로 복원합니다.
    /// (선생님이 설명할 때 학생들의 시선을 교탁으로 집중시킴)
    /// </summary>
    public static bool ToggleBlackScreen()
    {
        if (_currentSlideShowWindow == null) return false;
        try
        {
            // ppSlideShowRunning = 1, ppSlideShowBlackScreen = 3
            int state = (int)_currentSlideShowWindow.View.State;
            _currentSlideShowWindow.View.State = (state == 3) ? 1 : 3;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerPointPresentationHelper] ToggleBlackScreen 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 현재 화면이 암전(Blackout) 상태인지 여부.
    /// </summary>
    public static bool IsBlackScreen
    {
        get
        {
            if (_currentSlideShowWindow == null) return false;
            try
            {
                return (int)_currentSlideShowWindow.View.State == 3;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 현재 슬라이드 번호와 전체 슬라이드 수를 반환합니다. (실패 시 null)
    /// </summary>
    public static (int Current, int Total)? GetSlideInfo()
    {
        if (_currentSlideShowWindow == null) return null;
        try
        {
            int current = (int)_currentSlideShowWindow.View.Slide.SlideIndex;
            int total = (int)_currentSlideShowWindow.Presentation.Slides.Count;
            return (current, total);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 실행 중인 파워포인트 슬라이드 쇼를 종료합니다.
    /// </summary>
    public static bool CloseSlideShow()
    {
        if (_currentSlideShowWindow == null) return false;
        try
        {
            _currentSlideShowWindow.View.Exit();
            _currentSlideShowWindow = null;
            CurrentPresentationTitle = null;
            SlideShowStateChanged?.Invoke(false);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerPointPresentationHelper] CloseSlideShow 실패: {ex.Message}");
            _currentSlideShowWindow = null;
            CurrentPresentationTitle = null;
            SlideShowStateChanged?.Invoke(false);
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
            _currentSlideShowWindow = slideShowWindow;

            if (targetBounds is { } rect && slideShowWindow is not null)
            {
                try
                {
                    // 1. COM 레벨 기본 크기 지정
                    slideShowWindow.Left = (float)rect.Left;
                    slideShowWindow.Top = (float)rect.Top;
                    slideShowWindow.Width = (float)rect.Width;
                    slideShowWindow.Height = (float)rect.Height;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PowerPointPresentationHelper] COM 창 위치 조정 실패: {ex.Message}");
                }

                try
                {
                    // 2. Win32 HWND 정밀 픽셀 밀착 (DPI 배율 오차 방지)
                    int hwndVal = (int)slideShowWindow.HWND;
                    if (hwndVal != 0)
                    {
                        IntPtr hwnd = new IntPtr(hwndVal);
                        SetWindowPos(hwnd, IntPtr.Zero, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, SWP_NOACTIVATE | SWP_NOZORDER | SWP_SHOWWINDOW);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PowerPointPresentationHelper] Win32 SetWindowPos 밀착 실패: {ex.Message}");
                }
            }

            CurrentPresentationTitle = Path.GetFileName(pptFilePath);
            SlideShowStateChanged?.Invoke(true);
            return (true, "무간섭 파워포인트 슬라이드 쇼가 실행되었습니다.\n교탁 PC에 전용 PPT 원격 리모컨이 활성화되었습니다!");
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
