using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.Views.Windows;
using Microsoft.Win32;

namespace KnolTeacher.Desktop.Views.Controls.Widgets;

public partial class QrWidgetView : UserControl
{
    private const int LongContentWarningThreshold = 140;

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

    public static string NormalizeQrContent(string? rawText)
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

    public static bool ShouldWarnAboutLongContent(string? normalizedContent) =>
        !string.IsNullOrWhiteSpace(normalizedContent) &&
        normalizedContent.Length >= LongContentWarningThreshold;

    private void RenderQr()
    {
        if (ImgWidgetQr == null) return;

        string content = CurrentText;
        UpdateShareLinkPresentation(content);

        try
        {
            ImgWidgetQr.Source = _qrCodeService.GenerateQrBitmap(content, 12);
            ImgWidgetQr.ToolTip = content;
            if (TxtQrStatus != null)
            {
                TxtQrStatus.Text = "✓ QR 준비됨";
                TxtQrStatus.Foreground = Brushes.DarkGreen;
            }
        }
        catch (Exception ex)
        {
            ImgWidgetQr.Source = null;
            ImgWidgetQr.ToolTip = $"QR 코드를 만들 수 없습니다. ({ex.GetType().Name})";
            if (TxtQrStatus != null)
            {
                TxtQrStatus.Text = "QR 생성 실패 · 입력 내용을 확인해 주세요";
                TxtQrStatus.Foreground = Brushes.DarkRed;
            }
            System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Render failed: {ex.GetType().Name}");
        }
    }

    private void UpdateShareLinkPresentation(string content)
    {
        if (TbShareLink != null)
        {
            TbShareLink.Text = content;
            TbShareLink.ToolTip = content;
        }

        if (TxtLinkHint == null) return;

        if (ShouldWarnAboutLongContent(content))
        {
            TxtLinkHint.Text = "⚠ 입력 내용이 길어 QR 무늬가 복잡합니다. QR이 잘 안 찍히면 링크 복사를 사용하거나 원본 서비스의 짧은 공유 링크를 이용하세요.";
            TxtLinkHint.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
        }
        else
        {
            TxtLinkHint.Text = "QR을 찍기 어려운 학생에게는 이 링크를 그대로 복사해 전달할 수 있습니다.";
            TxtLinkHint.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
        }
    }

    private void TbWidgetUrl_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_renderTimer == null) return;

        string content = NormalizeQrContent(TbWidgetUrl?.Text);
        UpdateShareLinkPresentation(content);

        _renderTimer.Stop();
        _renderTimer.Start();
        if (TxtQrStatus != null)
        {
            TxtQrStatus.Text = "입력 후 자동 갱신 중...";
            TxtQrStatus.Foreground = Brushes.SlateGray;
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();
        RenderQr();
    }

    private async void BtnCopyLink_Click(object sender, RoutedEventArgs e)
    {
        string content = CurrentText;
        UpdateShareLinkPresentation(content);

        if (await TryCopyTextToClipboardAsync(content))
        {
            HudNotificationWindow.Instance.ShowToast("🔗", "학생에게 전달할 링크가 복사되었습니다.");
        }
        else
        {
            HudNotificationWindow.Instance.ShowToast("⚠️", "링크를 복사하지 못했습니다. 잠시 후 다시 시도해 주세요.");
        }
    }

    private async Task<bool> TryCopyTextToClipboardAsync(string text)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Link clipboard attempt {attempt + 1} failed: {ex.GetType().Name}");
                if (attempt < 2)
                {
                    await Task.Delay(60);
                }
            }
        }

        return false;
    }

    private void BtnCopyQrImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _qrCodeService.CopyQrToClipboard(CurrentText);
            HudNotificationWindow.Instance.ShowToast("📱", "QR 코드 이미지가 복사되었습니다.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Copy image failed: {ex.GetType().Name}");
            HudNotificationWindow.Instance.ShowToast("⚠️", "QR 코드를 복사하지 못했습니다. 잠시 후 다시 시도해 주세요.");
        }
    }

    private void BtnSavePng_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BitmapSource? bitmap = ImgWidgetQr.Source as BitmapSource;
            if (bitmap == null)
            {
                RenderQr();
                bitmap = ImgWidgetQr.Source as BitmapSource;
            }

            if (bitmap == null)
            {
                HudNotificationWindow.Instance.ShowToast("⚠️", "저장할 QR 코드가 없습니다.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "수업 QR 코드 저장",
                FileName = "놀티쳐-수업-QR.png",
                DefaultExt = ".png",
                AddExtension = true,
                Filter = "PNG 이미지 (*.png)|*.png"
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
            HudNotificationWindow.Instance.ShowToast("💾", "QR 코드 PNG를 저장했습니다.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Nolboard.QR] Save PNG failed: {ex.GetType().Name}");
            HudNotificationWindow.Instance.ShowToast("⚠️", "QR 코드를 저장하지 못했습니다.");
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
