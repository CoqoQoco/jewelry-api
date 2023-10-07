using Jewelry.Data.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public interface IFileExtension
    {
        Stream? GetPlanImage(string imageName);
        string GetPlanImageBase64String(string imageName);
    }
    public class FileExtension : IFileExtension
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;

        public FileExtension(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        public Stream? GetPlanImage(string imageName)
        {
            string folderPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/OrderPlan");
            string imagePath = Path.Combine(folderPath, imageName);

            if (System.IO.File.Exists(imagePath))
            {
                // เปิด FileStream สำหรับไฟล์ภาพ
                FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);

                // สร้าง FileStreamResult และส่งคืนไฟล์ภาพ
                //return File(fileStream, "image/jpeg"); // เปลี่ยน "image/jpeg" เป็นประเภทของไฟล์ภาพตามที่คุณใช้งาน

                return fileStream;
            }
            else
            {
                return null;
            }
        }
        public string GetPlanImageBase64String(string imageName)
        {
            //string folderPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/OrderPlan");
            string folderPath = Path.Combine("Images", "/OrderPlan");
            string imagePath = Path.Combine(folderPath, imageName);

            if (System.IO.File.Exists(imagePath))
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);

                // แปลง byte array เป็น Base64 string
                return Convert.ToBase64String(imageBytes);
            }
            else
            {
                return null;
            }
        }


    }
}
