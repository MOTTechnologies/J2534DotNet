namespace Plugins
{
    /// <summary>
    /// Plugin.FileFormat.*.dll provides an interface to open/close third party files
    /// </summary>
    public interface FileFormat
    {
        string FileExtension { get; }

        string FileDescription { get; }

        bool Open(string fileName);

        bool FileOpen { get; }

        void WriteBytes(byte[] bytes, string fileName);

        bool TryReadBytes(out byte[] bytes);

    }
}
