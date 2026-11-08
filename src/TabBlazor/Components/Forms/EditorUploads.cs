#nullable enable
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

/// <summary>
/// A file the user added to an editor (<see cref="MarkdownEditor"/>, or <c>RichTextEditor</c> from <c>TabBlazor.TipTap</c>)
/// by pasting, dropping or picking it. Read it with
/// <see cref="OpenReadStream"/>; the editor shows the upload progress from how far the stream has been read.
/// </summary>
public sealed class EditorUploadFile
{
    private readonly Func<CancellationToken, Stream> openReadStream;

    /// <summary>Creates a file, for example to test an upload handler.</summary>
    /// <param name="name">The file name, including the extension.</param>
    /// <param name="contentType">The MIME type, such as <c>image/png</c>. May be empty when the browser does not know it.</param>
    /// <param name="size">The size in bytes.</param>
    /// <param name="openReadStream">Opens the file content.</param>
    public EditorUploadFile(string name, string contentType, long size, Func<CancellationToken, Stream> openReadStream)
    {
        ArgumentNullException.ThrowIfNull(openReadStream);
        Name = name ?? "";
        ContentType = contentType ?? "";
        Size = size;
        this.openReadStream = openReadStream;
    }

    internal static EditorUploadFile FromBrowserFile(IBrowserFile file, long maxAllowedSize) =>
        new(file.Name, file.ContentType, file.Size, cancellationToken => file.OpenReadStream(maxAllowedSize, cancellationToken))
        {
            LastModified = file.LastModified
        };

    /// <summary>The file name as reported by the browser, including the extension. Do not use it as a storage path.</summary>
    public string Name { get; }

    /// <summary>
    /// The MIME type as reported by the browser. It is guessed from the file extension and may be empty, so check the
    /// content on the server before trusting it.
    /// </summary>
    public string ContentType { get; }

    /// <summary>The size in bytes.</summary>
    public long Size { get; }

    /// <summary>When the file was last modified, if known.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>Called with the number of bytes read so far.</summary>
    internal Action<long>? Progress { get; set; }

    /// <summary>Opens the file content. Read it to the end once; the editor's progress bar follows the read position.</summary>
    public Stream OpenReadStream(CancellationToken cancellationToken = default)
    {
        var stream = openReadStream(cancellationToken);
        return Progress is { } progress ? new ProgressStream(stream, progress) : stream;
    }

    private sealed class ProgressStream(Stream inner, Action<long> progress) : Stream
    {
        private long read;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Report(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer) => Report(inner.Read(buffer));

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Report(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Report(await inner.ReadAsync(buffer, cancellationToken));

        private int Report(int count)
        {
            if (count > 0)
            {
                read += count;
                progress(read);
            }
            return count;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}

/// <summary>Where an uploaded file ended up, returned from an editor's <c>UploadFile</c> handler.</summary>
/// <param name="Url">The URL the content links to. Relative URLs are fine.</param>
public sealed record EditorUploadResult(string Url)
{
    /// <summary>The image description or link text. Defaults to the file name (without extension for images).</summary>
    public string? Text { get; init; }

    /// <summary>
    /// True inserts an image, false a link. When null, images the browser can show (png, jpeg, gif, webp, svg, avif,
    /// bmp) are embedded and everything else is linked.
    /// </summary>
    public bool? Embed { get; init; }
}

/// <summary>
/// Throw from an editor's <c>UploadFile</c> handler to reject a file with a message the user sees,
/// for example "PDF files are limited to 2 MB". Other exceptions show a generic "Could not upload" message.
/// </summary>
public class EditorUploadException : Exception
{
    public EditorUploadException(string message) : base(message) { }
    public EditorUploadException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>An upload that failed or was rejected, raised through an editor's <c>OnUploadError</c>.</summary>
/// <param name="FileName">The name of the file.</param>
/// <param name="Message">The message shown to the user.</param>
/// <param name="Exception">The exception from the upload handler, or null when the editor rejected the file itself (size or type).</param>
public sealed record EditorUploadError(string FileName, string Message, Exception? Exception);

/// <summary>An upload in progress, shown in the editor's status list.</summary>
internal sealed class EditorUpload(string token, string fileName, long size) : IDisposable
{
    public string Token { get; } = token;
    public string FileName { get; } = fileName;
    public long Size { get; } = size;
    public long BytesRead { get; set; }
    public CancellationTokenSource Cancellation { get; } = new();
    public int Percent => Size > 0 ? (int)Math.Min(100, BytesRead * 100 / Size) : 0;
    public void Dispose() => Cancellation.Dispose();
}
