using QRCoder;
using SixLabors.ImageSharp.Formats;
using System.Drawing;
using System.Drawing.Imaging;

namespace Main;

public class QRCodeHelper
{
    public static string GenerateQRCodeBase64(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrCodeData);
        using var qrCodeImage = qrCode.GetGraphic(20);

        using var ms = new MemoryStream();
        qrCodeImage.Save(ms, ImageFormat.Png);
        byte[] byteImage = ms.ToArray();
        return Convert.ToBase64String(byteImage);
    }

    public static string GenerateRedemptionCode()
    {
        // Generate a unique 8-character code
        return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
    }
}