# trale-plugin-sdk
Interfaces, documentation and samples for making import/merge/export plugins for TRALE (.NET 10 versions).<br>
It does not include TRALE itself.

TRALE（.NET 10 版）向けの import/merge/export プラグインを作るためのインターフェース・説明・サンプルです。<br>
TRALE本体のソースコードは含みません。

旧版（.NET 6 版）向けの情報は [As-key/trale_plugin](https://github.com/As-key/trale_plugin) にあります。<br>
Information for the old (.NET 6) versions is in [As-key/trale_plugin](https://github.com/As-key/trale_plugin).

翻訳エンジン DLL の作り方は [As-key/trale-engine-sdk](https://github.com/As-key/trale-engine-sdk) を見てください。<br>
For translation engine DLLs, see [As-key/trale-engine-sdk](https://github.com/As-key/trale-engine-sdk).

### TRALE Web site :
https://trale.org/
<br><br>

# 2つのインターフェース / Two interfaces

| | IF1 `InterfaceTralePlugin` | IF2 `InterfaceTralePlugin2` |
|---|---|---|
| できること / Features | 原文の取り込み・訳文のマージ・1言語の書き出し<br>Import source, merge translations, export one language | IF1 に加えて、複数言語の取り込み・1ファイルへの複数言語の書き出し（TMX など）<br>IF1 plus multilingual import and multi-language export to one file (e.g. TMX) |
| 定義の持ち方 / How to get the definition | 定義（[interface/InterfaceTralePlugin.cs](interface/InterfaceTralePlugin.cs)）をプロジェクトにコピーしてよい<br>You may copy the definition into your project | TRALE フォルダの `Trale.Plugin.Abstractions.dll` を**参照する**（コピーしない）<br>**Reference** `Trale.Plugin.Abstractions.dll` from the TRALE folder (do not copy the source) |
| サンプル / Sample | [sample_if1/TRALE_Plugin_FCPXML.cs](sample_if1/TRALE_Plugin_FCPXML.cs) | [sample_if2/](sample_if2/) |

`InterfaceTralePlugin2` は `InterfaceTralePlugin` を継承しているので、IF2 のプラグインは IF1 のメソッドもすべて実装します。IF2 のプラグインは `Trale.Plugin.Abstractions.dll` が入っている TRALE（.NET 10 版）でだけ読み込まれます。<br>
`InterfaceTralePlugin2` extends `InterfaceTralePlugin`, so an IF2 plugin implements every IF1 method too. IF2 plugins load only in TRALE versions that ship `Trale.Plugin.Abstractions.dll` (the .NET 10 versions).

## TRALE がプラグインを見分ける方法 / How TRALE detects plugins

1. TRALE は起動時に `plugins` フォルダの `*.dll` を読み込み、公開された（public）・abstract でないクラスのうち、名前が `TralePluginManager.InterfaceTralePlugin` のインターフェースを実装したものをすべてプラグインとして登録します（1つの DLL に複数のプラグインを入れられます）。<br>
   At startup TRALE loads every `*.dll` in the `plugins` folder and registers each public, non-abstract class that implements an interface named `TralePluginManager.InterfaceTralePlugin` (one DLL may contain several plugins).
2. そのクラスが TRALE 本体の `Trale.Plugin.Abstractions.dll` にある `InterfaceTralePlugin2` を実装していれば IF2、そうでなければ IF1 として使います。IF1 は名前（FullName）で一致すればよいのですが、IF2 は**型そのもの**が一致する必要があります。<br>
   If the class implements `InterfaceTralePlugin2` from TRALE's own `Trale.Plugin.Abstractions.dll`, it is used as IF2; otherwise as IF1. IF1 only has to match by name (FullName), but IF2 must be **the same type**.
3. `InterfaceTralePlugin2` の定義をコピーしてビルドしたプラグインは、IF1 として使われ、読み込み時に警告が出ます。<br>
   A plugin that compiles a copy of `InterfaceTralePlugin2` is used as IF1, and TRALE reports a warning.

## 置き場所 / Where to put the files

作成した DLL を、TRALE 本体（`TranslationEditor.exe`）と同じ場所にある `plugins` フォルダに置き、TRALE を起動し直してください。<br>
Put the built DLL into the `plugins` folder next to `TranslationEditor.exe` and restart TRALE.

- `Trale.Plugin.Abstractions.dll` はプラグインと一緒に配布しないでください（TRALE にあるものが使われます）。<br>
  Do not ship `Trale.Plugin.Abstractions.dll` with your plugin; TRALE's own copy is used.
- プラグインが使うほかの DLL（NuGet パッケージなど）は、プラグインと同じ `plugins` フォルダに置いてください。<br>
  Put other DLLs your plugin needs (NuGet packages etc.) into the same `plugins` folder.

## ターゲットフレームワーク / Target framework

現在の TRALE は .NET 10 で動いています。プラグインは **`net10.0`** でビルドしてください（WinForms を使わないなら `-windows` は不要です）。<br>
TRALE runs on .NET 10. Build plugins for **`net10.0`** (`net10.0-windows` is not needed unless you use WinForms).
<br><br>

# IF2 サンプル / IF2 sample (`sample_if2`)

[sample_if2/TraleSampleTsvPlugin](sample_if2/TraleSampleTsvPlugin) は、1行目が言語コードの見出しになっているタブ区切りファイル（TSV）を扱う IF2 プラグインです。<br>
[sample_if2/TraleSampleTsvPlugin](sample_if2/TraleSampleTsvPlugin) is an IF2 plugin for a tab-separated file whose header row lists language codes.

    id          ja              en              fr
    title       はじめる         Start           Commencer
    msg_001     こんにちは\n世界  Hello\nworld

1列目が行の ID、2列目が原文、3列目以降が訳文です（例：[sample_if2/testdata/sample_multilingual.tsv](sample_if2/testdata/sample_multilingual.tsv)）。1つの DLL に2つのプラグインが入っています。<br>
Column 1 is the row id, column 2 the source, columns 3.. the translations. The DLL contains two plugins:

| プラグイン名 / Plugin name | `MultilingualMode` | 書き出し / Export |
|---|---|---|
| `Sample multilingual TSV (single file)` | `SingleFile` | 選んだ言語をまとめて1つの TSV に書く。TRALE は `ExportMultilingual` を1回呼ぶ。<br>Writes the selected languages into one TSV. TRALE calls `ExportMultilingual` once. |
| `Sample multilingual TSV (per language)` | `PerLanguageFile` | 1言語を1つの TSV に書く。TRALE は従来の `Export` を呼び、`CurrentData` の `Translate` 列にその言語の訳文が入る。<br>Writes one language per TSV. TRALE calls the old `Export`; the `Translate` column of `CurrentData` holds that language. |

どちらも `ImportMultilingual` で、原文と全言語の訳文をまとめて取り込めます。<br>
Both import the source and all target languages at once with `ImportMultilingual`.

## ビルド / Build

.NET 10 SDK が必要です。`TraleDir` に TRALE のフォルダ（`TranslationEditor.exe` と `Trale.Plugin.Abstractions.dll` があるフォルダ）を指定します。<br>
Requires the .NET 10 SDK. Set `TraleDir` to the TRALE folder (the folder with `TranslationEditor.exe` and `Trale.Plugin.Abstractions.dll`).

    cd sample_if2\TraleSampleTsvPlugin
    dotnet build -c Release -p:TraleDir="C:\path\to\TRALE"

`TraleDir` を省略した場合は `sample_if2\lib\Trale.Plugin.Abstractions.dll` を参照します（TRALE フォルダからコピーしてください）。<br>
Without `TraleDir`, the project references `sample_if2\lib\Trale.Plugin.Abstractions.dll` (copy it from the TRALE folder).

できた `bin\Release\net10.0\TraleSampleTsvPlugin.dll` を TRALE の `plugins` フォルダにコピーします。<br>
Copy `bin\Release\net10.0\TraleSampleTsvPlugin.dll` into TRALE's `plugins` folder.

.csproj で大事なのは次の部分です / The important part of the .csproj:

    <Reference Include="Trale.Plugin.Abstractions">
      <HintPath>$(TraleDir)\Trale.Plugin.Abstractions.dll</HintPath>
      <Private>false</Private>   <!-- 出力にコピーしない / do not copy to the output -->
    </Reference>

## IF2 のメソッド / IF2 methods

定義：[interface/InterfaceTralePlugin2.cs](interface/InterfaceTralePlugin2.cs)（参照用のコピー / reference copy）

### `TralePluginCapabilities GetCapabilities()`
対応機能を返します。項目は今後増えることがあるので、必要なものだけ設定してください。<br>
Returns the capabilities. New properties may be added; set only what you need.

| 項目 / Property | 意味 / Meaning |
|---|---|
| `InterfaceVersion` | `2` |
| `MultilingualMode` | `SingleFile`：複数言語を1ファイルに書く / several languages in one file. `PerLanguageFile`：1言語1ファイル / one file per language |
| `SupportsMultilingualExport` | `ExportMultilingual` に対応 / supports `ExportMultilingual` |
| `SupportsMultilingualImport` | `ImportMultilingual` に対応 / supports `ImportMultilingual` |
| `SupportedLanguages` | 扱える TRALE の言語コード。空なら制限なし / TRALE language codes the format supports; empty = any |
| `LanguageColumns` | 列で言語を分ける形式（xlsx など）の言語 → 列 / language → column for column-based formats |

### `bool ImportMultilingual(Stream file, out TraleMultilingualImport result)`
原文と、ファイルにあるすべての言語の訳文を取り込みます。<br>
Imports the source text and every translation in the file.

- `OriginalData`：従来の `Import` と同じ形（キー → 列の並び）。キーの順が行の順です。TRALE は 0 始まりで 2 列目（`[キー, ID, 原文]` の原文）を原文として使います。<br>
  Same shape as the old `Import` (key → columns), in row order. TRALE uses column index 2 as the source (`[key, id, source]`).
- `SourceLanguage`：ファイルから分かる原文の言語コード（分からなければ `null`）/ source language from the file, or `null`
- `Translations`：言語コード → (`OriginalData` のキー → 訳文)。空の訳は入れません。/ language → (key → text); leave out empty texts
- `Warnings`：取り込めたが利用者に伝えたいこと / non-fatal notes for the user

### `bool ExportMultilingual(TraleExportContext ctx)`
`MultilingualMode = SingleFile` のとき、選ばれた言語をまとめて1つのファイルに書きます。<br>
With `MultilingualMode = SingleFile`, writes all selected languages into one file.

- `ctx.Output`：出力先。閉じないでください（TRALE が閉じます）/ output stream; do not close it
- `ctx.FormatData`：取り込んだときの元ファイル（無ければ `null`）/ the originally imported file, or `null`
- `ctx.SourceLanguage`, `ctx.TargetLanguages`：原文と訳文の言語コード / language codes
- `ctx.Rows`：行順の `Guid` と `Source` / rows in project order
- `ctx.Translations`：言語コード → (GUID → 訳文)。訳の無い行はキーがありません / language → (GUID → text); untranslated rows have no key
- `ctx.Options`：プラグイン設定 / plugin settings

### IF1 のメソッドについて / About the IF1 methods
IF2 のプラグインも IF1 のメソッドを全部実装します。`PerLanguageFile` のプラグインでは、書き出しに従来の `Export` が使われます。<br>
An IF2 plugin implements every IF1 method too. `PerLanguageFile` plugins export through the old `Export`.
<br><br>

# IF1（従来のインターフェース / old interface）

作成したpluginファイルは、TRALE本体と同じ場所にあるpluginsフォルダに格納してください。TRALE起動時に自動的に定義情報が読み込まれます。</br>

## Plugin interface
Plugin interfaceにそって必要な機能を実装する必要があります。<br>

    namespace TralePluginManager
    {
        public interface InterfaceTralePlugin
        {
            public string GetPluginName();
            public string DefaultName();
            public string ExtentionFilter();
            public void SupportFunction(out bool Import, out bool Merge, out bool Export);
            public bool Import(FileStream ImportFile, out Dictionary<string, List<string>>  OriginalData, out Dictionary<string, List<string>> TranslateData);
            public bool Merge(FileStream MergeFile, ReadOnlyCollection<DataRow> CurrentData, out Dictionary<string, List<string>> TranslateData);
            public bool Export(FileStream ExportFile, ReadOnlyCollection<DataRow> CurrentData, FileStream OriginalFile);
        }
    }
<br>

## public string GetPluginName()
        public string GetPluginName()
        {
            return "plugin for FCPXML";
        }

TRALE上で表示されるPluginの名称を定義します。<br>
他のPluginと競合しないように気を付けてください。<br>
<br>

## public string DefaultName()
        public string DefaultName()
        {
            return "fcpxml_default.fcpxml";
        }

デフォルトファイル名を定義します。<br>
<br>

## public string ExtentionFilter()
        public string ExtentionFilter()
        {
            return "FCPXML files(*.fcpxml)|*.fcpxml|All files(*.*)|*.*";
        }

ファイルダイアログに設定する拡張子情報を定義します。<br>
<br>

## public void SupportFunction(out bool Import, out bool Merge, out bool Export)
        public void SupportFunction(out bool Import, out bool Merge, out bool Export)
        {
            Import = true;
            Merge = false;
            Export = true;
        }

Pluginの機能情報を定義します。
Pluginがサポートする機能にTrueを設定してください。<br>
<br>

## public bool Import(...)

    public bool Import(
        FileStream ImportFile, 
        out Dictionary<string, List<string>> OriginalData, 
        out Dictionary<string, List<string>> TranslateData)

原文Import用のinterfaceです。<br>

    FileStream ImportFile : 
        TRALE上でユーザーが選択したインポートファイルのファイルストリーム

    out Dictionary<string, List<string>> OriginalData :
        原文を格納する為のDictionary型の変数です。
        キーの順が行の順になります。値は列の並びで、TRALE は 0 始まりで 2 列目を原文として使います。

    out Dictionary<string, List<string>> TranslateData :
        訳文を格納する為のDictionary型の変数です。
<br>

## public bool Merge(...)
    public bool Merge(
        FileStream MergeFile,
        ReadOnlyCollection<DataRow> CurrentData,
        out Dictionary<string, List<string>> TranslateData);

訳文Merge用のinterfaceです。<br>

    FileStream MergeFile :
        TRALE上でユーザーが選択したマージ用ファイルのファイルストリーム

    ReadOnlyCollection<DataRow> CurrentData :
        TRALEのメモリ上に展開されている現在のデータです。
        主な列："GUID", "Source", "Preprocess", "Translate", "Re-Translate"

    out Dictionary<string, List<string>> TranslateData :
        TRALE側に渡すマージ用の訳文データです。

<br>

## public bool Export(...)
    public bool Export(
        FileStream ExportFile,
        ReadOnlyCollection<DataRow> CurrentData,
        FileStream OriginalFile);

Export（オリジナルデータに訳文をマージして新規ファイル保存）用のinterfaceです。<br>

    FileStream ExportFile :
        エクスポートデータの書き込み先となるファイルストリームです。

    ReadOnlyCollection<DataRow> CurrentData :
        TRALEのメモリ上に展開されている現在のデータです（行は取り込み順）。
        主な列："GUID", "Source", "Preprocess", "Translate", "Re-Translate"

    FileStream OriginalFile :
        プロジェクト内に保存されている、オリジナルデータです。
        このデータを元に、エクスポートデータの生成を行ってください。
<br>
