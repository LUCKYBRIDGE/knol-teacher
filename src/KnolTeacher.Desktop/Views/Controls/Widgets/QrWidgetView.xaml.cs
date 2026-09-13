using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.Views.Windows;

namespace KnolTeacher.Desktop.Views.Controls.Widgets;

public partial class QrWidgetView : UserControl
{
    private static readonly Regex DomainLikeInput = new(
        @"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,62}\.)+[A-Za-z]{2,}(?::\d+)?(?:[/#?].*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IQrCodeService _qrCodeService;
    private readonly DispatcherTimer _renderTimer;

    public QrWidgetView(IQrCodeService? qrCodeService = null)
    {
        _qrCodeService = qrCodeService ?? new QrCodeService();
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _renderTimer.Tick += (_, _) =>
        {
            _renderTimer.Stop();
            RenderQr();
        };

        InitializeComponent();
        Loaded += (_, _) => RenderQr();
        Unloaded += (_, _) => _renderTimer.Stop();
    }

    private string CurrentText => NormalizeQrContent(TbWidgetUrl.Text);

    internal static string NormalizeQrContent(string? rawText)
    {
        string text = rawText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return "https://pinky-ne.com/";
        }

        if (Uri.TryCreate(text, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return text;
        }

        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || DomainLikeInput.IsMatch(text))
        {
            return $"https://{text}";
        }

        return text;
    }

    private void RenderQr()
    {
        if (ImgWidgetQr == null) return;

        try
        {
            ImgWidgetQr.Source = _qrCodeService.GenerateQrBitmap(CurrentText, 12);
            ImgWidgetQr.ToolTip = CurrentText;
            if (TxtQrStatus != null)
            {
                TxtQrStatus.Text = "✓ QR 준비됨";
                TxtQrStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
            }
        }
        catch (Exception ex)
        {
            ImgWidgetQr.Source = null;
            ImgWidgetQr.ToolTip = $"QR 코드를 만들 수 없습니다. ({ex.GetType().Name})";
            if (TxtQrStatus != null)
            {
                TxtQrStatus.Text = "QR 생성 실패 · 입력 내용을 확인해 주세요";
                TxtQrStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
            }
            System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Render failed: {ex.GetType().Name}");
        }
    }

    private void TbWidgetUrl_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_renderTimer == null) return;
        _renderTimer.Stop();
        _renderTimer.Start();
        if (TxtQrStatus != null)
        {
            TxtQrStatus.Text = "입력 후 자동 갱신 중...";
            TxtQrStatus.Foreground = System.Windows.Media.Brushes.SlateGray;
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();
        RenderQr();
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _qrCodeService.CopyQrToClipboard(CurrentText);
            HudNotificationWindow.Instance.ShowToast("📱", "QR 코드 이미지가 복사되었습니다.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Copy failed: {ex.GetType().Name}");
            HudNotificationWindow.Instance.ShowToast("⚠️", "QR 코드를 복사하지 못했습니다. 잠시 후 다시 시도해 주세요.");
        }
    }

    private void BtnZoom_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new QrCodeModalDialog(_qrCodeService, "📱 실시간 수업 QR 코드", CurrentText)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Zoom failed: {ex.GetType().Name}");
            HudNotificationWindow.Instance.ShowToast("⚠️", "QR 코드를 크게 표시하지 못했습니다.");
        }
    }
}
