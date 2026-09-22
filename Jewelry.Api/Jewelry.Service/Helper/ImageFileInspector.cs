namespace Jewelry.Service.Helper;

public class ImageInspectResult
{
    public string Extension { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public int Width { get; set; }
    public int Height { get; set; }
}

public static class ImageFileInspector
{
    public static bool TryInspect(byte[] bytes, out ImageInspectResult? result)
    {
        result = null;

        if (bytes == null || bytes.Length < 12)
        {
            return false;
        }

        if (IsJpeg(bytes))
        {
            if (!TryReadJpegDimensions(bytes, out var width, out var height))
            {
                return false;
            }

            result = new ImageInspectResult { Extension = "jpg", ContentType = "image/jpeg", Width = width, Height = height };
            return true;
        }

        if (IsPng(bytes))
        {
            if (!TryReadPngDimensions(bytes, out var width, out var height))
            {
                return false;
            }

            result = new ImageInspectResult { Extension = "png", ContentType = "image/png", Width = width, Height = height };
            return true;
        }

        if (IsWebp(bytes))
        {
            if (!TryReadWebpDimensions(bytes, out var width, out var height))
            {
                return false;
            }

            result = new ImageInspectResult { Extension = "webp", ContentType = "image/webp", Width = width, Height = height };
            return true;
        }

        return false;
    }

    // ===== Magic bytes =====

    private static bool IsJpeg(byte[] b)
        => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

    private static bool IsPng(byte[] b)
        => b.Length >= 8
            && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A;

    private static bool IsWebp(byte[] b)
        => b.Length >= 12
            && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
            && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P';

    // ===== PNG: IHDR chunk ต้องเป็น chunk แรกเสมอ (offset คงที่) =====
    // 8 byte signature + 4 byte chunk length + 4 byte "IHDR" + 4 byte width + 4 byte height (big-endian)

    private static bool TryReadPngDimensions(byte[] b, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (b.Length < 24)
        {
            return false;
        }

        if (!(b[12] == (byte)'I' && b[13] == (byte)'H' && b[14] == (byte)'D' && b[15] == (byte)'R'))
        {
            return false;
        }

        width = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        height = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        return width > 0 && height > 0;
    }

    // ===== JPEG: เดิน marker segment (FF xx LL LL ...) จนเจอ SOFn (ยกเว้น DHT/JPG/DAC) =====

    private static bool TryReadJpegDimensions(byte[] b, out int width, out int height)
    {
        width = 0;
        height = 0;

        var offset = 2; // ข้าม SOI (FF D8)

        while (offset + 1 < b.Length)
        {
            if (b[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = b[offset + 1];

            // FF ซ้ำกัน (fill byte) — ขยับไปทีละ 1 เพื่อหา marker byte ถัดไป
            if (marker == 0xFF)
            {
                offset++;
                continue;
            }

            // marker ที่ไม่มี length segment: SOI/EOI/RST0-7/TEM
            if (marker == 0x01 || marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
            {
                offset += 2;
                continue;
            }

            if (offset + 4 > b.Length)
            {
                break;
            }

            var segmentLength = (b[offset + 2] << 8) | b[offset + 3];

            var isSof = marker >= 0xC0 && marker <= 0xCF
                && marker != 0xC4 // DHT
                && marker != 0xC8 // JPG (reserved)
                && marker != 0xCC; // DAC

            if (isSof)
            {
                if (offset + 9 > b.Length)
                {
                    break;
                }

                height = (b[offset + 5] << 8) | b[offset + 6];
                width = (b[offset + 7] << 8) | b[offset + 8];
                return width > 0 && height > 0;
            }

            if (marker == 0xDA) // SOS — เข้าสู่ scan data แล้ว ไม่มี SOF ก่อนหน้า ถือว่า parse ไม่ได้
            {
                break;
            }

            offset += 2 + segmentLength;
        }

        return false;
    }

    // ===== WebP: RIFF....WEBP + chunk แรก (VP8 / VP8L / VP8X) =====

    private static bool TryReadWebpDimensions(byte[] b, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (b.Length < 30)
        {
            return false;
        }

        var fourCc = System.Text.Encoding.ASCII.GetString(b, 12, 4);

        if (fourCc == "VP8 ")
        {
            // data (offset 20): 3 byte frame tag, 3 byte start code (9D 01 2A), 2+2 byte width/height (14-bit, little-endian)
            if (!(b[23] == 0x9D && b[24] == 0x01 && b[25] == 0x2A))
            {
                return false;
            }

            width = (b[26] | (b[27] << 8)) & 0x3FFF;
            height = (b[28] | (b[29] << 8)) & 0x3FFF;
            return width > 0 && height > 0;
        }

        if (fourCc == "VP8L")
        {
            // data (offset 20): 1 byte signature (0x2F), 4 byte bit-packed width-1(14bit)/height-1(14bit)/...
            if (b[20] != 0x2F)
            {
                return false;
            }

            var bits = b[21] | (b[22] << 8) | (b[23] << 16) | (b[24] << 24);
            width = (bits & 0x3FFF) + 1;
            height = ((bits >> 14) & 0x3FFF) + 1;
            return true;
        }

        if (fourCc == "VP8X")
        {
            // data (offset 20): 1 byte flags, 3 byte reserved, 3 byte canvas width-1, 3 byte canvas height-1 (little-endian)
            width = (b[24] | (b[25] << 8) | (b[26] << 16)) + 1;
            height = (b[27] | (b[28] << 8) | (b[29] << 16)) + 1;
            return true;
        }

        return false;
    }
}
