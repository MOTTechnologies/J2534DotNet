using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace J2534DotNet.Logger
{
    class FormattedStreamWriter: StreamWriter
    {
        private readonly IFormatProvider formatProvider = new NullFormat();

        public FormattedStreamWriter(Stream stream) : base(stream)
        {
        }

        public FormattedStreamWriter(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public FormattedStreamWriter(Stream stream, Encoding encoding, int bufferSize) : base(stream, encoding, bufferSize)
        {
        }

        public FormattedStreamWriter(string path) : base(path)
        {
        }

        public FormattedStreamWriter(string path, bool append) : base(path, append)
        {
        }

        public FormattedStreamWriter(string path, bool append, Encoding encoding) : base(path, append, encoding)
        {
        }

        public FormattedStreamWriter(string path, bool append, Encoding encoding, int bufferSize) : base(path, append, encoding, bufferSize)
        {
        }

        public override IFormatProvider FormatProvider
        {
            get { return formatProvider; }
        }
    }
}
