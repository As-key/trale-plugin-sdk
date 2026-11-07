using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using TralePluginManager;

namespace TraleSampleTsvPlugin
{
    // ============================================================================================
    // InterfaceTralePlugin2 のサンプル（多言語 TSV）
    // Sample of InterfaceTralePlugin2 (multilingual TSV)
    //
    // 扱うファイル / File format (UTF-8, tab separated, first line = header):
    //
    //     id        ja        en         fr
    //     title     はじめる   Start      Commencer
    //     msg_001   こんにちは\n世界   Hello\nworld
    //
    //   1列目 = 行の ID、2列目 = 原文の言語、3列目以降 = 訳文の言語（列名は TRALE の言語コード）。
    //   セル内のタブ・改行・\ は \t \n \\ と書く。
    //   Column 1 = row id, column 2 = source language, columns 3.. = target languages (TRALE language codes).
    //   Tabs, line breaks and backslashes in a cell are written as \t \n \\.
    //
    // 1つの DLL に2つのプラグインを入れている（TRALE は DLL 内の公開クラスをすべて読み込む）。
    // This DLL contains two plugins (TRALE loads every public plugin class in a DLL):
    //   - SampleTsvSingleFilePlugin   : MultilingualMode = SingleFile
    //       複数言語を1ファイルに書く。TRALE は ExportMultilingual を1回だけ呼ぶ。
    //       Writes all selected languages into one file. TRALE calls ExportMultilingual once.
    //   - SampleTsvPerLanguagePlugin  : MultilingualMode = PerLanguageFile
    //       1言語1ファイル。TRALE は言語ごとに従来の Export を呼ぶ（CurrentData の Translate 列がその言語の訳文）。
    //       One file per language. TRALE calls the old Export once per language
    //       (the Translate column of CurrentData holds that language's translation).
    //   どちらも ImportMultilingual で、原文と全言語の訳文をまとめて取り込める。
    //   Both import the source text and every target language at once with ImportMultilingual.
    // ============================================================================================

    /// <summary>2つのプラグインに共通の処理。abstract クラスは TRALE に読み込まれない。</summary>
    public abstract class SampleTsvPluginBase : InterfaceTralePlugin2
    {
        // ---------------------------------------------------------------- InterfaceTralePlugin（従来のIF / old IF）

        /// <summary>TRALE 上の表示名。ほかのプラグインと重ならない名前にする。/ Display name; must be unique.</summary>
        public abstract string GetPluginName();

        public string DefaultName()
        {
            return "multilingual.tsv";
        }

        public string ExtentionFilter()
        {
            return "Tab separated values(*.tsv;*.txt)|*.tsv;*.txt|All files(*.*)|*.*";
        }

        public void SupportFunction(out bool Import, out bool Merge, out bool Export)
        {
            Import = true;
            Merge = false;
            Export = true;
        }

        /// <summary>
        /// 従来の取り込み（原文だけ）。IF2 に対応した TRALE は ImportMultilingual を使うが、従来の経路のために残す。
        /// Old import (source text only). TRALE uses ImportMultilingual, but keep this for the old path.
        /// OriginalData: key → [key, id, source]（原文は 0 始まりで 2 列目 / the source is column index 2）。
        /// </summary>
        public bool Import(FileStream ImportFile, out Dictionary<string, List<string>> OriginalData, out Dictionary<string, List<string>> TranslateData)
        {
            TranslateData = new Dictionary<string, List<string>>();
            bool ok = ImportMultilingual(ImportFile, out TraleMultilingualImport result);
            OriginalData = result.OriginalData;
            return ok;
        }

        public bool Merge(FileStream MergeFile, ReadOnlyCollection<DataRow> CurrentData, out Dictionary<string, List<string>> TranslateData)
        {
            TranslateData = new Dictionary<string, List<string>>();
            return false;
        }

        /// <summary>
        /// 従来の書き出し（1言語）。PerLanguageFile のプラグインでは、TRALE が言語ごとにこれを呼ぶ。
        /// Old export (one language). For PerLanguageFile plugins TRALE calls this once per language.
        /// CurrentData の列: "GUID", "Source", "Translate" など（行は取り込み順）。
        /// OriginalFile: 取り込んだときの元ファイル（プロジェクトの original/format.dat）。
        /// </summary>
        public bool Export(FileStream ExportFile, ReadOnlyCollection<DataRow> CurrentData, FileStream OriginalFile)
        {
            try
            {
                List<string> ids = ReadIds(OriginalFile, CurrentData.Count);
                using var writer = CreateWriter(ExportFile);
                writer.Write("id\ttext\n");
                for (int i = 0; i < CurrentData.Count; i++)
                {
                    DataRow row = CurrentData[i];
                    string translation = row["Translate"]?.ToString() ?? "";
                    // 訳が無い行は原文を書く / Untranslated rows keep the source text
                    string text = translation.Length > 0 ? translation : row["Source"]?.ToString() ?? "";
                    string id = ids.Count > i ? ids[i] : row["GUID"]?.ToString() ?? "";
                    writer.Write(Escape(id) + "\t" + Escape(text) + "\n");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------------- InterfaceTralePlugin2（拡張IF / extended IF）

        /// <summary>
        /// 対応機能。項目は今後増えることがあるので、必要なものだけ設定する（ほかは既定値）。
        /// Capabilities. New properties may be added later; set only what you need.
        /// </summary>
        public abstract TralePluginCapabilities GetCapabilities();

        /// <summary>
        /// 原文と全言語の訳文を取り込む。Translations のキーは TRALE の言語コード（ja, en, fr ...）。
        /// Imports the source and all target languages. Translations is keyed by TRALE language code.
        /// 戻り値 false は失敗（TRALE がエラーを表示）。取り込めたが注意があるときは Warnings に入れる。
        /// Return false on failure. Put non-fatal notes into Warnings.
        /// </summary>
        public bool ImportMultilingual(Stream file, out TraleMultilingualImport result)
        {
            var original = new Dictionary<string, List<string>>();
            var translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            result = new TraleMultilingualImport { OriginalData = original, Translations = translations, Warnings = warnings };
            try
            {
                List<string[]> table = ReadTable(file);
                if (table.Count == 0 || table[0].Length < 2)
                {
                    return false;
                }
                string[] header = table[0];
                string sourceLanguage = header[1].Trim();

                for (int line = 1; line < table.Count; line++)
                {
                    string[] cells = table[line];
                    // キーは行の並びを表す文字列（TRALE は取り込み順を使う）/ The key keeps the row order
                    string key = (line - 1).ToString("000000");
                    string id = cells.Length > 0 ? cells[0] : "";
                    string source = cells.Length > 1 ? cells[1] : "";
                    original.Add(key, new List<string> { key, id, source });

                    for (int column = 2; column < header.Length; column++)
                    {
                        string language = header[column].Trim();
                        string text = cells.Length > column ? cells[column] : "";
                        if (language.Length == 0 || text.Length == 0)
                        {
                            continue;   // 空の訳は入れない / Skip empty translations
                        }
                        if (!translations.TryGetValue(language, out Dictionary<string, string>? texts))
                        {
                            texts = new Dictionary<string, string>(StringComparer.Ordinal);
                            translations[language] = texts;
                        }
                        texts[key] = text;
                    }
                }

                if (original.Count == 0)
                {
                    warnings.Add("The file has no rows.");
                }
                result = new TraleMultilingualImport
                {
                    OriginalData = original,
                    SourceLanguage = sourceLanguage.Length == 0 ? null : sourceLanguage,
                    Translations = translations,
                    Warnings = warnings
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 複数言語を1つのファイルに書き出す（MultilingualMode = SingleFile のときに TRALE が呼ぶ）。
        /// Writes several languages into one file (called when MultilingualMode = SingleFile).
        /// ctx.Rows は行順、ctx.Translations は 言語 → (GUID → 訳文)。訳の無い行はキー自体が無い。
        /// ctx.Rows is in row order; ctx.Translations is language → (GUID → text); untranslated rows have no key.
        /// ctx.Output は TRALE が閉じるので、プラグインは閉じない。/ Do not close ctx.Output.
        /// </summary>
        public bool ExportMultilingual(TraleExportContext ctx)
        {
            try
            {
                List<string> ids = ReadIds(ctx.FormatData, ctx.Rows.Count);
                using var writer = CreateWriter(ctx.Output);
                var header = new List<string> { "id", ctx.SourceLanguage.Length > 0 ? ctx.SourceLanguage : "source" };
                header.AddRange(ctx.TargetLanguages);
                writer.Write(string.Join("\t", header.Select(Escape)) + "\n");

                for (int i = 0; i < ctx.Rows.Count; i++)
                {
                    TraleExportRow row = ctx.Rows[i];
                    var cells = new List<string> { ids.Count > i ? ids[i] : row.Guid, row.Source };
                    foreach (string language in ctx.TargetLanguages)
                    {
                        string text = ctx.Translations.TryGetValue(language, out IReadOnlyDictionary<string, string>? texts) &&
                                      texts.TryGetValue(row.Guid, out string? value) ? value : "";
                        cells.Add(text);
                    }
                    writer.Write(string.Join("\t", cells.Select(Escape)) + "\n");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------------- helpers

        // 出力先のストリームは閉じない（TRALE が閉じる）/ Leave the stream open (TRALE closes it)
        private static StreamWriter CreateWriter(Stream output)
        {
            return new StreamWriter(output, new UTF8Encoding(false), 4096, leaveOpen: true);
        }

        private static List<string[]> ReadTable(Stream input)
        {
            var rows = new List<string[]>();
            using var reader = new StreamReader(input, Encoding.UTF8, true, 4096, leaveOpen: true);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                rows.Add(line.Split('\t').Select(Unescape).ToArray());
            }
            return rows;
        }

        // 元ファイル（format.dat）の ID を行順に読む。行数が合わなければ使わない（GUID を ID にする）。
        // Reads the row ids of the original file in order; ignored when the row count differs.
        private static List<string> ReadIds(Stream? formatData, int rowCount)
        {
            if (formatData == null)
            {
                return new List<string>();
            }
            List<string> ids = ReadTable(formatData).Skip(1).Select(cells => cells.Length > 0 ? cells[0] : "").ToList();
            return ids.Count == rowCount ? ids : new List<string>();
        }

        // TRALE 内の改行は \r\n / Line breaks inside TRALE are \r\n
        private static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
        }

        private static string Unescape(string value)
        {
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\' && i + 1 < value.Length)
                {
                    char next = value[++i];
                    builder.Append(next switch { 'n' => "\r\n", 't' => "\t", '\\' => "\\", _ => "\\" + next });
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }

    /// <summary>複数言語を1つの TSV に書く。/ Writes all languages into one TSV.</summary>
    public sealed class SampleTsvSingleFilePlugin : SampleTsvPluginBase
    {
        public override string GetPluginName()
        {
            return "Sample multilingual TSV (single file)";
        }

        public override TralePluginCapabilities GetCapabilities()
        {
            return new TralePluginCapabilities
            {
                InterfaceVersion = 2,
                MultilingualMode = TraleMultilingualMode.SingleFile,
                SupportsMultilingualExport = true,
                SupportsMultilingualImport = true
                // SupportedLanguages を空にすると言語の制限なし / Empty SupportedLanguages = any language
            };
        }
    }

    /// <summary>言語ごとに別の TSV を書く（取り込みは多言語）。/ One TSV per language (multilingual import).</summary>
    public sealed class SampleTsvPerLanguagePlugin : SampleTsvPluginBase
    {
        public override string GetPluginName()
        {
            return "Sample multilingual TSV (per language)";
        }

        public override TralePluginCapabilities GetCapabilities()
        {
            return new TralePluginCapabilities
            {
                InterfaceVersion = 2,
                MultilingualMode = TraleMultilingualMode.PerLanguageFile,
                SupportsMultilingualExport = true,
                SupportsMultilingualImport = true
            };
        }
    }
}
