using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace KnolTeacher.Desktop.Services;

public interface IQrCodeService
{
    BitmapSource GenerateQrBitmap(string content, int pixelsPerModule = 10);
    byte[] GenerateQrPngBytes(string content, int pixelsPerModule = 10);
    void CopyQrToClipboard(string content);
    void SaveQrToFile(string content, string filePath);
}

public class QrCodeService : IQrCodeService
{
    public BitmapSource GenerateQrBitmap(string content, int pixelsPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(content)) content = "https://knolteacher.com";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

        var image = new BitmapImage();
        using var mem = new MemoryStream(qrCodeBytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = mem;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public byte[] GenerateQrPngBytes(string content, int pixelsPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(content)) content = "https://knolteacher.com";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    public void CopyQrToClipboard(string content)
    {
        var bitmap = GenerateQrBitmap(content, 16);
        Exception? lastError = null;

        // The Windows clipboard can be briefly locked by another process. A few short
        // retries make classroom use much more reliable without hiding real failures.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetImage(bitmap);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt < 2)
                {
                    Thread.Sleep(60);
                }
            }
        }

        throw new InvalidOperationException("QR image could not be copied to the Windows clipboard.", lastError);
    }

    public void SaveQrToFile(string content, string filePath)
    {
        byte[] bytes = GenerateQrPngBytes(content, 16);
        File.WriteAllBytes(filePath, bytes);
    }
}
