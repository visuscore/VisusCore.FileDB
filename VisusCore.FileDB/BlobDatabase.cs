using System;
using System.IO;
using VisusCore.FileDB.Helpers;
using VisusCore.FileDB.Structure;

namespace VisusCore.FileDB;

/// <summary>
/// FileDB main class.
/// </summary>
public partial class BlobDatabase : IDisposable
{
    private FileStream _fileStream;
    private Engine _engine;
    private DebugFile _debug;
    private bool _isDisposed;

    /// <summary>
    /// Gets debug information about FileDB Structure.
    /// </summary>
    public DebugFile Debug
    {
        get
        {
            _debug ??= new DebugFile(_engine);

            return _debug;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobDatabase"/> class.
    /// </summary>
    /// <param name="fileName">Database filename (eg: C:\Data\MyDB.dat).</param>
    /// <param name="fileAccess">Acces mode (Read|ReadWrite|Write).</param>
    public BlobDatabase(string fileName, FileAccess fileAccess) =>
        Connect(fileName, fileAccess);

    private void Connect(string fileName, FileAccess fileAccess)
    {
        if (!File.Exists(fileName))
            CreateEmptyFile(fileName);

        // Não permite acesso somente gravação (transforma em leitura/gravação)
        var fa = fileAccess is FileAccess.Write or FileAccess.ReadWrite
            ? FileAccess.ReadWrite
            : FileAccess.Read;

        _fileStream = new FileStream(
            fileName,
            FileMode.Open,
            fa,
            FileShare.ReadWrite,
            (int)BasePage.PageSize,
            FileOptions.None);

        _engine = new Engine(_fileStream);
    }

    /// <summary>
    /// Store a disk file inside database.
    /// </summary>
    /// <param name="fileName">Full path to file (eg: C:\Temp\MyPhoto.jpg).</param>
    /// <returns>EntryInfo with information store.</returns>
    public EntryInfo Store(string fileName)
    {
        using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

        return Store(fileName, stream);
    }

    /// <summary>
    /// Store a stream inside database.
    /// </summary>
    /// <param name="fileName">Just a name of file, to get future reference (eg: MyPhoto.jpg).</param>
    /// <param name="input">Stream thats contains the file.</param>
    /// <returns>EntryInfo with information store.</returns>
    public EntryInfo Store(string fileName, Stream input)
    {
        var entry = new EntryInfo(fileName);
        _engine.Write(entry, input);

        return entry;
    }

    internal void Store(EntryInfo entry, Stream input) =>
        _engine.Write(entry, input);

    /// <summary>
    /// Retrieve a file inside a database.
    /// </summary>
    /// <param name="id">A Guid that references to file.</param>
    /// <param name="fileName">Path to save the file.</param>
    /// <returns>EntryInfo with information about the file.</returns>
    public EntryInfo Read(Guid id, string fileName)
    {
        using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

        return Read(id, stream);
    }

    /// <summary>
    /// Retrieve a file inside a database.
    /// </summary>
    /// <param name="id">A Guid that references to file.</param>
    /// <param name="output">Output strem to save the file.</param>
    /// <returns>EntryInfo with information about the file.</returns>
    public EntryInfo Read(Guid id, Stream output) =>
        _engine.Read(id, output);

    /// <summary>
    /// Retrieve a file inside a database returning a FileDBStream to read.
    /// </summary>
    /// <param name="id">A Guid that references to file.</param>
    /// <returns>A FileDBStream ready to be readed or null if ID was not found.</returns>
    public BlobDatabaseStream OpenRead(Guid id) =>
        _engine.OpenRead(id);

    /// <summary>
    /// Search for a file inside database BUT get only EntryInfo information (don't copy the file).
    /// </summary>
    /// <param name="id">File ID.</param>
    /// <returns>EntryInfo with file information or null with not found.</returns>
    public EntryInfo Search(Guid id)
    {
        var indexNode = _engine.Search(id);

        if (indexNode == null)
        {
            return null;
        }

        return new EntryInfo(indexNode);
    }

    /// <summary>
    /// Delete a file inside database.
    /// </summary>
    /// <param name="id">Guid ID from a file.</param>
    /// <returns>True when the file was deleted or False when not found.</returns>
    public bool Delete(Guid id) =>
        _engine.Delete(id);

    /// <summary>
    /// List all files inside a FileDB.
    /// </summary>
    /// <returns>Array with all files.</returns>
    public EntryInfo[] ListFiles() =>
        _engine.ListAllFiles();

    /// <summary>
    /// Export all files inside FileDB database to a directory.
    /// </summary>
    /// <param name="directory">Directory name.</param>
    public void Export(string directory) =>
        Export(directory, "{filename}.{id}.{extension}");

    /// <summary>
    /// Export all files inside FileDB database to a directory.
    /// </summary>
    /// <param name="directory">Directory name.</param>
    /// <param name="filePattern">File Pattern. Use keys: {id} {extension} {filename}. Eg: "{filename}.{id}.{extension}".</param>
    public void Export(string directory, string filePattern)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var files = ListFiles();

        foreach (var file in files)
        {
            var fileName = filePattern.Replace("{id}", file.ID.ToString())
                .Replace("{filename}", Path.GetFileNameWithoutExtension(file.FileName))
                .Replace("{extension}", Path.GetExtension(file.FileName).Replace(".", string.Empty));

            Read(file.ID, Path.Combine(directory, fileName));
        }
    }

    /// <summary>
    /// Shrink datafile.
    /// </summary>
    public void Shrink()
    {
        var dbFileName = _fileStream.Name;
        var fileAccess = _fileStream.CanWrite ? FileAccess.ReadWrite : FileAccess.Read;
        var tempFile = Path.Combine(
            Path.GetDirectoryName(dbFileName),
            $"{Path.GetFileNameWithoutExtension(dbFileName)}.temp{Path.GetExtension(dbFileName)}");

        if (File.Exists(tempFile))
            File.Delete(tempFile);

        var entries = ListFiles();

        CreateEmptyFile(tempFile, ignoreIfExists: false);

        using (var tempDb = new BlobDatabase(tempFile, FileAccess.ReadWrite))
        {
            foreach (var entry in entries)
            {
                using var stream = new MemoryStream();

                Read(entry.ID, stream);
                stream.Seek(0, SeekOrigin.Begin);
                tempDb.Store(entry, stream);
            }
        }

        Dispose();

        File.Delete(dbFileName);
        File.Move(tempFile, dbFileName);

        Connect(dbFileName, fileAccess);
    }

    public void Persist()
    {
        _engine?.PersistPages();
        if (_fileStream.CanWrite)
            _fileStream.Flush();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing && _engine is not null)
            {
                _engine.PersistPages(); // Persiste as paginas/header que ficaram em memória

                if (_fileStream.CanWrite)
                    _fileStream.Flush();

                _engine.Dispose();

                _fileStream.Dispose();
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
