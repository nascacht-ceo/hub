The `nc-extensions-streaming` package extends `System.IO.Stream` to enable:

- Determining a `MimeType` from the first few bytes of a stream
- Comparing streams for cryptographic, visual, or semantic similarity

# Determine MimeType

The `stream.GetMimeType()` extension method will inspect the first few bytes of a stream.
Common mime types can be determined from these first few bytes, including:


attempting to determine a `MimeType` based on the "magic bytes", as follows:

|MimeType|Bytes|
|-|-|
|`images/jpeg`|`0xFF, 0xD8, 0xFF`|


Usage:

```csharp
using var stream = new Stream("/path/to/some/file");
var mimetype = stream.GetMimeType();
```


## Microsoft Office Documents

Office documents (`.docx`, `.xlsx`, `.pptx`, etc) are saved as compressed zip files,
thus their magic bytes match that of a zip file. 
If you wish to determine whether a stream is a specific Office file:

```csharp
using var stream = new Stream("/path/to/some/file");
var mimetype = stream.GetMimeType(includeMSOffice = true);
// or, just
var mimetype = stream.GetMimeType(true);
```

This operation may prove expensive, as the entire stream will be read and unzipped 
using `System.IO.Compression.ZipArchive` to inspect entry names.

# Fingerprinting

Similarities between streams can be determined via:

|Hashing Method|Details|
|-|-|
|Cryptographic|A crytographic hash is a 100% reliable method to detect files are are identical. File that differ by a single bit will result in a different hash, so cryptographic hashing is only useful for detecting identical streams.|
|Visual|A visual hash is determined from the bitmap of an image. Hamming distance can be used to determine similarity of images.|
|Semantic|A semantic hash is determined from the words contained in a stream of text. Hamming distance can be used to determine similarity of language content.|

Usage:

```csharp
using var streamA = new Stream("/path/to/some/file");
using var streamB = new Stream("/path/to/other/file");
var fingerprintA = streamA.ToFingerprint();
var fingerprintB = streamB.ToFingerprint();
if (fingerprintA.CryptographicHash == fingerprintB.CryptographicHash)
  Console.Log("These files are identical.");
```

## Comparing hashes with Hamming distance

The `Fingerprint.Compare()` method will return a `FingerPrintMatch` based on Hamming distance:

```csharp
var match = FingerprintCompare(fingerprintA, fingerprintB);
switch (match)
{
  case FingerprintMatch.Exact:
    Console.Log("These hashes are identical.");
    break;
  case FingerprintMatch.Duplicate:
    Console.Log("These hashes so close that the files are very likely duplicates.");
    break;
  case FingerprintMatch.Similar:
    Console.Log("These hashes are close enough that the files may be duplicates.");
    break;
  case FingerprintMatch.Different:
    Console.Log("These hashes are different enough that the files are likely different.");
}
```

