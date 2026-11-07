using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using TralePluginManager;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections;

namespace TRALE_Plugin_FCPXML
{
    public class TRALE_Plugin_FCPXML : InterfaceTralePlugin
    {
        public string GetPluginName()
        {
            return "plugin for FCPXML";
        }

        public string DefaultName()
        {
            return "fcpxml_default.fcpxml";
        }

        public string ExtentionFilter()
        {
            return "FCPXML files(*.fcpxml)|*.fcpxml|All files(*.*)|*.*";
        }

        public void SupportFunction(out bool Import, out bool Merge, out bool Export)
        {
            Import = true;
            Merge = false;
            Export = true;
        }

        public bool Import(FileStream ImportFile, out Dictionary<string, List<string>> OriginalData, out Dictionary<string, List<string>> TranslateData)
        {
            bool ret = false;
            OriginalData = new Dictionary<string, List<string>>();
            TranslateData = new Dictionary<string, List<string>>();

            try
            {
                int cnt = 0;
                XmlDocument doc = new XmlDocument();
                doc.Load(ImportFile);
                XmlNodeList list = doc.GetElementsByTagName("text-style");
                foreach (XmlNode node in list)
                {
                    if (node.ParentNode.NodeType == XmlNodeType.Element && node.ParentNode.Name == "text")
                    {
                        var id = cnt.ToString("000000");
                        List<string> words = new List<string>();
                        words.Add(id.ToString());
                        words.Add(node.ParentNode.InnerXml.ToString());
                        words.Add(node.InnerText);
                        words.Add("");
                        OriginalData.Add(id.ToString(), words);
                    }
                    cnt++;
                }
                ret = true;
            }
            catch
            {
                ret = false;
            }
            return ret;
        }

        public bool Merge(FileStream MergeFile, ReadOnlyCollection<DataRow> CurrentData, out Dictionary<string, List<string>> TranslateData)
        {
            TranslateData = new Dictionary<string, List<string>>();
            return false;
        }

        public bool Export(FileStream ExportFile, ReadOnlyCollection<DataRow> CurrentData, FileStream FormatData)
        {
            bool ret = false;
            try
            {
                int cnt = 0;
                XmlDocument doc = new XmlDocument();
                doc.Load(FormatData);
                XmlNodeList list = doc.GetElementsByTagName("text-style");
                foreach (XmlNode node in list)
                {
                    if (node.ParentNode.NodeType == XmlNodeType.Element && node.ParentNode.Name == "text")
                    {
                        if (node.InnerText.ToString().Trim() == CurrentData[cnt]["Source"].ToString().Trim())
                        {
                            if (CurrentData[cnt]["Translate"].ToString() != "")
                            {
                                node.InnerText = CurrentData[cnt]["Translate"].ToString();
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                        cnt++;
                    }
                }
                doc.Save(ExportFile);
                ret = true;
            }
            catch
            {
                ret = false;
            }
            return ret;
        }
    }
}