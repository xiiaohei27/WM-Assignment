using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Main;

public class QRCodeHelper
{
    public static string GenerateQRCodeBase64(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        byte[] qrCodeBytes = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeBytes);
    }

    public static string GenerateRedemptionCode()
    {
        // Generate a unique 8-character code
        return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
    }
}