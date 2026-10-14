# Files — real document fixtures for the integration suite

Copied verbatim from the upstream
[GroupDocs.Search-for-.NET](https://github.com/groupdocs-search/GroupDocs.Search-for-.NET) repository,
folder `Examples/Resources/Documents/` — the set the upstream "Build your first search solution" and
basic-search examples index. At fixture start-up they are copied into `<storage>/samples/`.

| File | Used by | Why |
|---|---|---|
| `Lorem ipsum.pdf` | `SearchTests.RealSamples_AreIndexedAcrossFormats` | PDF text extraction during indexing |
| `Lorem ipsum.docx` | same | DOCX text extraction during indexing |
| `Lorem ipsum.txt` | same | Plain text; all three Lorem files contain "lorem" |
| `English.docx` | indexed with the samples | Second DOCX, larger vocabulary |
| `English.txt` | indexed with the samples | Larger plain-text body |

Assertions about which document matches which word use **synthetic** files written per test
(`SampleDocuments.AlphaText` / `BetaText`), never the content of these samples.

Total size ~260 KB. Not included on purpose: the upstream password-protected documents (the server has
no password parameter yet) and `DocumentsPNG/sample.png` (2 MB; image search is not exposed).

## Refresh

```bash
cp <GroupDocs.Search-for-.NET>/Examples/Resources/Documents/* ./Files/
```
