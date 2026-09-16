using System.IO;

namespace Jewelry.Service.Helper
{
    public static class StockImagePath
    {
        private const string StockProductFolder = "Stock/Product";

        // กรณี legacy migrated SKU: ImagePath คือชื่อไฟล์ ("{NoCode}.jpg") อยู่ใต้โฟลเดอร์ Stock/Product เสมอ
        // กรณีอัปโหลด/แทนที่รูปผ่าน ProductImageService.Replace: ImagePath คือโฟลเดอร์เต็ม ("Stock/Product") + ImageName คือชื่อไฟล์
        public static string? Build(string? imagePath, string? imageName)
        {
            if (string.IsNullOrEmpty(imagePath) && string.IsNullOrEmpty(imageName))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(imagePath) && imagePath.Contains('/'))
            {
                if (string.IsNullOrEmpty(imageName))
                {
                    return null;
                }

                return $"{imagePath.TrimEnd('/')}/{imageName}";
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                return $"{StockProductFolder}/{imagePath}";
            }

            var fileName = imageName!;
            if (!Path.HasExtension(fileName))
            {
                fileName = $"{fileName}.jpg";
            }

            return $"{StockProductFolder}/{fileName}";
        }
    }
}
