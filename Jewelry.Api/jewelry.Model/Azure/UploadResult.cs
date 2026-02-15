namespace jewelry.Model.Azure;

/// <summary>
/// ผลลัพธ์การ Upload
/// </summary>
public class UploadResult
{
    public bool Success { get; set; }
    public string Url { get; set; }
    public string BlobName { get; set; }
    public string ErrorMessage { get; set; }
}
