// 参照用のコピーです。プロジェクトにコピーしてビルドしないでください（型が一致せず、TRALE は IF2 として検出しません）。
// TRALE フォルダの Trale.Plugin.Abstractions.dll を参照してください（sample_if2 の .csproj を参照）。
// Reference copy only. Do NOT compile a copy of this file into your plugin: TRALE would not detect it as IF2.
// Reference Trale.Plugin.Abstractions.dll from the TRALE folder instead (see the .csproj in sample_if2).

using System;
using System.Collections.Generic;
using System.IO;

namespace TralePluginManager
{
    /// <summary>
    /// 入出力プラグインの拡張IF（後方互換あり）。1つのファイルに複数言語を書く形式（TMX など）向け。
    /// 本体は <c>plugin is InterfaceTralePlugin2</c> で判定する。実装していないプラグインには、
    /// 本体が言語ごとに既存の <see cref="InterfaceTralePlugin.Export"/> を繰り返し呼ぶ（段階1）。
    /// 実装するプラグインは Trale.Plugin.Abstractions を参照すること（定義のコピーでは型が一致せず検出されない）。
    /// </summary>
    public interface InterfaceTralePlugin2 : InterfaceTralePlugin
    {
        /// <summary>プラグインが対応している機能。</summary>
        TralePluginCapabilities GetCapabilities();

        /// <summary>複数言語をまとめて1つのファイルに書き出す。</summary>
        bool ExportMultilingual(TraleExportContext ctx);

        /// <summary>複数言語を含むファイルを取り込む。</summary>
        bool ImportMultilingual(Stream file, out TraleMultilingualImport result);
    }

    /// <summary>多言語をどう扱うか。</summary>
    public enum TraleMultilingualMode
    {
        /// <summary>1ファイル1言語（既存の Export を言語ごとに繰り返す）。</summary>
        PerLanguageFile = 0,

        /// <summary>1ファイルに複数言語を書く（<see cref="InterfaceTralePlugin2.ExportMultilingual"/> を使う）。</summary>
        SingleFile = 1
    }

    /// <summary>プラグインの対応機能。項目は今後追加されうるので、既定値で動くように実装すること。</summary>
    public sealed class TralePluginCapabilities
    {
        /// <summary>実装しているIFの版。<see cref="InterfaceTralePlugin2"/> は 2。</summary>
        public int InterfaceVersion { get; init; } = 2;

        public TraleMultilingualMode MultilingualMode { get; init; } = TraleMultilingualMode.SingleFile;

        public bool SupportsMultilingualExport { get; init; }

        public bool SupportsMultilingualImport { get; init; }

        /// <summary>扱える言語コード（TRALE の言語コード）。空なら制限なし。</summary>
        public IReadOnlyList<string> SupportedLanguages { get; init; } = Array.Empty<string>();

        /// <summary>
        /// 言語ごとの列の割り当て（xlsx など列で言語を分ける形式向け。キー=言語コード、値=列名や列番号の表記）。
        /// 列を使わない形式では空。
        /// </summary>
        public IReadOnlyDictionary<string, string> LanguageColumns { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>書き出す1行分の情報。並びはプロジェクトの行順（GUID 順）。</summary>
    public sealed class TraleExportRow
    {
        public string Guid { get; init; } = "";

        public string Source { get; init; } = "";
    }

    /// <summary><see cref="InterfaceTralePlugin2.ExportMultilingual"/> に渡す情報。</summary>
    public sealed class TraleExportContext
    {
        /// <summary>出力先。プラグインは閉じなくてよい（本体が閉じる）。</summary>
        public Stream Output { get; init; } = Stream.Null;

        /// <summary>取り込み時にプラグインが保存した元フォーマット（original/format.dat）。無い場合は null。</summary>
        public Stream? FormatData { get; init; }

        /// <summary>原文の言語コード。</summary>
        public string SourceLanguage { get; init; } = "";

        /// <summary>書き出す訳文の言語コード（指定順）。</summary>
        public IReadOnlyList<string> TargetLanguages { get; init; } = Array.Empty<string>();

        /// <summary>行（GUID 順）。</summary>
        public IReadOnlyList<TraleExportRow> Rows { get; init; } = Array.Empty<TraleExportRow>();

        /// <summary>訳文。キー=言語コード → (キー=GUID → 訳文)。訳が無い行はキー自体が無い。</summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations { get; init; }
            = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        /// <summary>プラグイン設定（plugin settings の内容）。</summary>
        public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
    }

    /// <summary><see cref="InterfaceTralePlugin2.ImportMultilingual"/> の結果。</summary>
    public sealed class TraleMultilingualImport
    {
        /// <summary>既存の Import の OriginalData と同じ形（キー=GUID → 列の並び）。</summary>
        public Dictionary<string, List<string>> OriginalData { get; init; } = new Dictionary<string, List<string>>();

        /// <summary>原文の言語コード（ファイルから分かる場合）。</summary>
        public string? SourceLanguage { get; init; }

        /// <summary>訳文。キー=言語コード → (キー=GUID → 訳文)。</summary>
        public Dictionary<string, Dictionary<string, string>> Translations { get; init; }
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>取り込みは成功したが利用者に伝えるべき事項（空の訳、不明な言語など）。</summary>
        public List<string> Warnings { get; init; } = new List<string>();
    }
}
