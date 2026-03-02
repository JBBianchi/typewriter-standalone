using Typewriter.Metadata;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class FileImpl : File
    {
        private readonly IFileMetadata _metadata;

        public FileImpl(IFileMetadata metadata, Settings settings)
        {
            _metadata = metadata;
            Settings = settings;
        }

        public Settings Settings { get; }

        public override string Name => _metadata.Name;

        public override string FullName => _metadata.FullName;

        private IClassCollection? _classes;

        public override IClassCollection Classes => _classes ?? (_classes = ClassImpl.FromMetadata(_metadata.Classes, this, Settings));

        private IRecordCollection? _records;

        public override IRecordCollection Records => _records ?? (_records = RecordImpl.FromMetadata(_metadata.Records, this, Settings));

        private IDelegateCollection? _delegates;

        public override IDelegateCollection Delegates => _delegates ?? (_delegates = DelegateImpl.FromMetadata(_metadata.Delegates, this, Settings));

        private IEnumCollection? _enums;

        public override IEnumCollection Enums => _enums ?? (_enums = EnumImpl.FromMetadata(_metadata.Enums, this, Settings));

        private IInterfaceCollection? _interfaces;

        public override IInterfaceCollection Interfaces => _interfaces ?? (_interfaces = InterfaceImpl.FromMetadata(_metadata.Interfaces, this, Settings));

        public override string ToString()
        {
            return Name;
        }
    }
}
