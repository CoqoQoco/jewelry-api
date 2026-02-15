namespace jewelry.Model.Azure;

/// <summary>
/// Configuration สำหรับ Azure Storage
/// </summary>
public class AzureStorageConfig
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public List<string> AllowedFolders { get; set; }
}
