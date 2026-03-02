using System;

namespace Typewriter.Metadata;

public interface IMetadataProvider
{
    IFileMetadata GetFile(string path, Settings settings, Action<string[]> requestRender);
}
