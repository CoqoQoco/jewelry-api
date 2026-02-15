namespace jewelry.Model.Azure;

/// <summary>
/// ข้อมูลของไฟล์ใน Blob Storage
/// </summary>
public class BlobFileInfo
{
    /// <summary>
    /// ชื่อไฟล์เท่านั้น เช่น "image1.jpg"
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// ชื่อ folder เช่น "Mold"
    /// </summary>
    public string FolderName { get; set; }

    /// <summary>
    /// Path เต็ม เช่น "Mold/image1.jpg"
    /// </summary>
    public string FullPath { get; set; }

    /// <summary>
    /// URL เต็มสำหรับเข้าถึงไฟล์
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// ขนาดไฟล์ (bytes)
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// วันที่แก้ไขล่าสุด
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }
}
