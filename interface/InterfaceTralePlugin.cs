using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;

namespace TralePluginManager
{
    public interface InterfaceTralePlugin
    {
        public string GetPluginName();
        public string DefaultName();
        public string ExtentionFilter();
        public void SupportFunction(out bool Import, out bool Merge, out bool Export);
        public bool Import(FileStream ImportFile, out Dictionary<string, List<string>> OriginalData, out Dictionary<string, List<string>> TranslateData);
        public bool Merge(FileStream MergeFile, ReadOnlyCollection<DataRow> CurrentData, out Dictionary<string, List<string>> TranslateData);
        public bool Export(FileStream ExportFile, ReadOnlyCollection<DataRow> CurrentData, FileStream OriginalFile);
    }
}
