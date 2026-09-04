using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace OrdTarifManager.Core
{
    public class TariffEngine
    {
        public XDocument CurrentDoc { get; private set; }
        public string CurrentFilePath { get; private set; }
        public XElement ParameterTemplate { get; set; }

        public TariffEngine()
        {
        }

        public DataTable CreateTariff(string tariffName, bool isCost, bool isVolume, int orderKind, DateTime validFrom, DateTime validTo, out TariffMetadata meta)
        {
            string spec = (isCost ? "(TC)" : "") + "Stepped" + (isVolume ? "Volume" : "Weight") + "DistanceConsolidation";
            string unitCode = isVolume ? "CBM" : "kg";
            string idUnit = isVolume ? "9" : "1";
            string idOrderKind = orderKind.ToString(CultureInfo.InvariantCulture);

            meta = new TariffMetadata();
            meta.Id = "NEW_TARIFF";
            meta.Name = !string.IsNullOrWhiteSpace(tariffName) ? tariffName.Trim() : "New Tariff";
            meta.Spec = spec;
            meta.CurrencyCode = "EUR";
            meta.UnitCode = unitCode;
            meta.OrderKind = orderKind;
            meta.ValidFrom = validFrom.ToString("yyyy-MM-dd");
            meta.ValidTo = validTo.ToString("yyyy-MM-dd");

            // Build XML Tree
            XElement root = new XElement("comtec", new XAttribute("version", "2014"));
            XElement resTariff = new XElement("resource_tariff",
                new XElement("id", meta.Id),
                new XElement("code", meta.Id),
                new XElement("name", meta.Name),
                new XElement("valid_from_date", meta.ValidFrom),
                new XElement("valid_till_date", meta.ValidTo),
                new XElement("price_kind_code", "Erlös")
            );
            root.Add(resTariff);

            XElement tariffItems = new XElement("tariff_items");
            resTariff.Add(tariffItems);

            XElement tariffItem = new XElement("tariff_item",
                new XElement("name", meta.Name),
                new XElement("tariff_item_spec", meta.Spec),
                new XElement("currency_code", meta.CurrencyCode),
                new XElement("valid_from_date", meta.ValidFrom),
                new XElement("valid_till_date", meta.ValidTo),
                new XElement("unit_code", meta.UnitCode)
            );
            tariffItems.Add(tariffItem);

            XElement tuplesContainer = new XElement("parameter_tuples");
            tariffItem.Add(tuplesContainer);

            // Columns sequence
            List<string> columns;
            if (isVolume)
            {
                columns = new List<string> { "minDistance", "maxDistance", "minVolume", "maxVolume", "price", "id_unit", "id_orderkind", "minPrice" };
            }
            else
            {
                columns = new List<string> { "minDistance", "maxDistance", "minWeight", "maxWeight", "price", "id_unit", "id_orderkind", "minPrice" };
            }

            // Create Seed Tuple
            XElement seedTuple = new XElement("parameter_tuple");
            foreach (string col in columns)
            {
                string val = "0.00";
                if (col == "id_unit") val = idUnit;
                else if (col == "id_orderkind") val = idOrderKind;

                seedTuple.Add(new XElement("parameter",
                    new XElement("code", col),
                    new XElement("value", val)
                ));
            }

            ParameterTemplate = new XElement(seedTuple);
            CurrentDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
            CurrentFilePath = null;

            // Return Empty DataTable with columns configured
            DataTable dt = new DataTable();
            foreach (string col in columns)
            {
                dt.Columns.Add(col, typeof(string));
            }

            return dt;
        }

        public DataTable CreateFromDefinition(string definitionName, out TariffMetadata meta)
        {
            bool isCost = definitionName != null && definitionName.IndexOf("(TC)", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isVolume = definitionName != null && definitionName.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0;
            return CreateTariff("New Tariff", isCost, isVolume, 2, DateTime.Today, new DateTime(2099, 12, 31), out meta);
        }

        public bool LoadTemplate(string filePath, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                CurrentFilePath = filePath;
                CurrentDoc = XDocument.Load(filePath);

                // Extract parameter template
                XElement tuplesContainer = CurrentDoc.Descendants("parameter_tuples").FirstOrDefault();
                if (tuplesContainer != null)
                {
                    XElement firstTuple = tuplesContainer.Element("parameter_tuple");
                    if (firstTuple != null)
                    {
                        ParameterTemplate = new XElement(firstTuple);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }

        public TariffMetadata GetMetadata()
        {
            var meta = new TariffMetadata();
            if (CurrentDoc == null || CurrentDoc.Root == null)
            {
                return meta;
            }

            XElement resTariff = CurrentDoc.Root.Element("resource_tariff");
            if (resTariff != null)
            {
                XElement idEl = resTariff.Element("id");
                if (idEl != null) meta.Id = idEl.Value;

                XElement nameEl = resTariff.Element("name");
                if (nameEl != null) meta.Name = nameEl.Value;

                XElement vfEl = resTariff.Element("valid_from_date");
                if (vfEl != null) meta.ValidFrom = vfEl.Value;

                XElement vtEl = resTariff.Element("valid_till_date");
                if (vtEl != null) meta.ValidTo = vtEl.Value;
            }

            XElement itemSpec = CurrentDoc.Descendants("tariff_item_spec").FirstOrDefault();
            if (itemSpec != null) meta.Spec = itemSpec.Value;

            XElement curCode = CurrentDoc.Descendants("currency_code").FirstOrDefault();
            if (curCode != null) meta.CurrencyCode = curCode.Value;

            XElement unitCode = CurrentDoc.Descendants("unit_code").FirstOrDefault();
            if (unitCode != null) meta.UnitCode = unitCode.Value;

            return meta;
        }

        public DataTable ExtractTuplesCheckSchema(out List<string> schema)
        {
            schema = new List<string>();
            DataTable dt = new DataTable();

            if (CurrentDoc == null || CurrentDoc.Root == null)
            {
                return dt;
            }

            XElement tuplesContainer = CurrentDoc.Descendants("parameter_tuples").FirstOrDefault();
            if (tuplesContainer == null)
            {
                return dt;
            }

            var allTuples = tuplesContainer.Elements("parameter_tuple").ToList();
            if (allTuples.Count == 0)
            {
                if (ParameterTemplate != null)
                {
                    foreach (XElement param in ParameterTemplate.Elements("parameter"))
                    {
                        XElement codeEl = param.Element("code");
                        if (codeEl != null && !schema.Contains(codeEl.Value))
                        {
                            schema.Add(codeEl.Value);
                            dt.Columns.Add(codeEl.Value, typeof(string));
                        }
                    }
                }
                return dt;
            }

            // Infer schema from first tuple
            XElement firstTuple = allTuples[0];
            foreach (XElement param in firstTuple.Elements("parameter"))
            {
                XElement codeEl = param.Element("code");
                if (codeEl != null && !schema.Contains(codeEl.Value))
                {
                    schema.Add(codeEl.Value);
                    dt.Columns.Add(codeEl.Value, typeof(string));
                }
            }

            // Extract all rows
            dt.BeginLoadData();
            foreach (XElement tuple in allTuples)
            {
                DataRow row = dt.NewRow();
                foreach (XElement param in tuple.Elements("parameter"))
                {
                    XElement codeEl = param.Element("code");
                    XElement valEl = param.Element("value");
                    if (codeEl != null && schema.Contains(codeEl.Value))
                    {
                        string val = valEl != null ? valEl.Value : "";
                        row[codeEl.Value] = val;
                    }
                }
                dt.Rows.Add(row);
            }
            dt.EndLoadData();

            return dt;
        }

        public DataTable GetTuplesAsDataTable()
        {
            List<string> schema;
            return ExtractTuplesCheckSchema(out schema);
        }

        public void UpdateMetadata(TariffMetadata meta)
        {
            if (CurrentDoc == null || CurrentDoc.Root == null || meta == null)
            {
                return;
            }

            XElement resTariff = CurrentDoc.Root.Element("resource_tariff");
            if (resTariff != null)
            {
                if (!string.IsNullOrEmpty(meta.Name))
                {
                    SetOrAddElement(resTariff, "name", meta.Name);
                    SetOrAddElement(resTariff, "id", meta.Name);
                    SetOrAddElement(resTariff, "code", meta.Name);
                    foreach (XElement item in CurrentDoc.Descendants("tariff_item"))
                    {
                        SetOrAddElement(item, "name", meta.Name);
                    }
                }

                if (!string.IsNullOrEmpty(meta.ValidFrom))
                {
                    SetOrAddElement(resTariff, "valid_from_date", meta.ValidFrom);
                    foreach (XElement item in CurrentDoc.Descendants("tariff_item"))
                    {
                        SetOrAddElement(item, "valid_from_date", meta.ValidFrom);
                    }
                }

                if (!string.IsNullOrEmpty(meta.ValidTo))
                {
                    SetOrAddElement(resTariff, "valid_till_date", meta.ValidTo);
                    foreach (XElement item in CurrentDoc.Descendants("tariff_item"))
                    {
                        SetOrAddElement(item, "valid_till_date", meta.ValidTo);
                    }
                }

                if (!string.IsNullOrEmpty(meta.Spec))
                {
                    foreach (XElement item in CurrentDoc.Descendants("tariff_item"))
                    {
                        SetOrAddElement(item, "tariff_item_spec", meta.Spec);
                    }

                    SetOrAddElement(resTariff, "price_kind_code", "Erlös");

                    bool isVolume = meta.Spec.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0;
                    foreach (XElement item in CurrentDoc.Descendants("tariff_item"))
                    {
                        SetOrAddElement(item, "unit_code", isVolume ? "CBM" : "kg");
                    }
                }

                if (!string.IsNullOrEmpty(meta.Id))
                {
                    SetOrAddElement(resTariff, "id", meta.Id);
                    SetOrAddElement(resTariff, "code", meta.Id);
                }
            }
        }

        public void ApplySpecificationChange(DataTable dt, bool isCost, bool isVolume, int orderKind, string newSpecName)
        {
            if (dt != null)
            {
                // 1. Rename columns if unit changed
                if (isVolume)
                {
                    if (dt.Columns.Contains("minWeight") && !dt.Columns.Contains("minVolume"))
                    {
                        dt.Columns["minWeight"].ColumnName = "minVolume";
                    }
                    if (dt.Columns.Contains("maxWeight") && !dt.Columns.Contains("maxVolume"))
                    {
                        dt.Columns["maxWeight"].ColumnName = "maxVolume";
                    }
                    if (dt.Columns.Contains("id_unit"))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            if (row.RowState != DataRowState.Deleted) row["id_unit"] = "9";
                        }
                    }
                }
                else
                {
                    if (dt.Columns.Contains("minVolume") && !dt.Columns.Contains("minWeight"))
                    {
                        dt.Columns["minVolume"].ColumnName = "minWeight";
                    }
                    if (dt.Columns.Contains("maxVolume") && !dt.Columns.Contains("maxWeight"))
                    {
                        dt.Columns["maxVolume"].ColumnName = "maxWeight";
                    }
                    if (dt.Columns.Contains("id_unit"))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            if (row.RowState != DataRowState.Deleted) row["id_unit"] = "1";
                        }
                    }
                }

                // 2. Update order kind in DataTable
                SetOrderKind(dt, orderKind);
            }

            // 3. Update XML elements
            if (CurrentDoc != null && CurrentDoc.Root != null)
            {
                XElement resTariff = CurrentDoc.Root.Element("resource_tariff");
                if (resTariff != null)
                {
                    SetOrAddElement(resTariff, "price_kind_code", "Erlös");
                }

                foreach (XElement item in CurrentDoc.Descendants("tariff_item"))
                {
                    SetOrAddElement(item, "tariff_item_spec", newSpecName);
                    SetOrAddElement(item, "unit_code", isVolume ? "CBM" : "kg");
                }

                if (ParameterTemplate != null)
                {
                    foreach (XElement param in ParameterTemplate.Elements("parameter"))
                    {
                        XElement codeEl = param.Element("code");
                        XElement valEl = param.Element("value");
                        if (codeEl != null)
                        {
                            if (isVolume)
                            {
                                if (codeEl.Value == "minWeight") codeEl.Value = "minVolume";
                                if (codeEl.Value == "maxWeight") codeEl.Value = "maxVolume";
                                if (codeEl.Value == "id_unit" && valEl != null) valEl.Value = "9";
                            }
                            else
                            {
                                if (codeEl.Value == "minVolume") codeEl.Value = "minWeight";
                                if (codeEl.Value == "maxVolume") codeEl.Value = "maxWeight";
                                if (codeEl.Value == "id_unit" && valEl != null) valEl.Value = "1";
                            }

                            if (codeEl.Value == "id_orderkind" && valEl != null)
                            {
                                valEl.Value = orderKind.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateTuples(DataTable dt)
        {
            if (CurrentDoc == null || CurrentDoc.Root == null)
            {
                return;
            }

            XElement tuplesContainer = CurrentDoc.Descendants("parameter_tuples").FirstOrDefault();
            if (tuplesContainer == null)
            {
                XElement tariffItem = CurrentDoc.Descendants("tariff_item").FirstOrDefault();
                if (tariffItem != null)
                {
                    tuplesContainer = new XElement("parameter_tuples");
                    tariffItem.Add(tuplesContainer);
                }
                else
                {
                    return;
                }
            }

            var allTuples = tuplesContainer.Elements("parameter_tuple").ToList();
            XElement templateTuple = null;
            if (ParameterTemplate != null)
            {
                templateTuple = ParameterTemplate;
            }
            else if (allTuples.Count > 0)
            {
                templateTuple = new XElement(allTuples[0]);
                ParameterTemplate = templateTuple;
            }
            else
            {
                return;
            }

            // Sync templateTuple with dt columns (e.g. minWeight <-> minVolume)
            if (dt != null && templateTuple != null)
            {
                foreach (XElement param in templateTuple.Elements("parameter"))
                {
                    XElement codeEl = param.Element("code");
                    if (codeEl != null)
                    {
                        if ((codeEl.Value == "minWeight" || codeEl.Value == "maxWeight") && dt.Columns.Contains("minVolume"))
                        {
                            codeEl.Value = codeEl.Value == "minWeight" ? "minVolume" : "maxVolume";
                        }
                        else if ((codeEl.Value == "minVolume" || codeEl.Value == "maxVolume") && dt.Columns.Contains("minWeight"))
                        {
                            codeEl.Value = codeEl.Value == "minVolume" ? "minWeight" : "maxWeight";
                        }
                    }
                }

                // Ensure all dt columns exist in templateTuple
                foreach (DataColumn col in dt.Columns)
                {
                    bool exists = false;
                    foreach (XElement param in templateTuple.Elements("parameter"))
                    {
                        XElement codeEl = param.Element("code");
                        if (codeEl != null && codeEl.Value == col.ColumnName)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        templateTuple.Add(new XElement("parameter",
                            new XElement("code", col.ColumnName),
                            new XElement("value", "0.00")
                        ));
                    }
                }
            }

            // Clear existing tuples
            tuplesContainer.RemoveNodes();

            if (dt == null)
            {
                return;
            }

            // Rebuild from DataTable
            foreach (DataRow row in dt.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                XElement newTuple = new XElement(templateTuple);
                foreach (XElement param in newTuple.Elements("parameter"))
                {
                    XElement codeEl = param.Element("code");
                    XElement valEl = param.Element("value");
                    if (codeEl != null && valEl != null)
                    {
                        string code = codeEl.Value;
                        if (dt.Columns.Contains(code))
                        {
                            object rawVal = row[code];
                            valEl.Value = NumberParser.FormatDisplay(rawVal, code);
                        }
                    }
                }
                tuplesContainer.Add(newTuple);
            }
        }

        public void SaveToFile(string outputPath)
        {
            if (CurrentDoc == null) return;

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.Encoding = Encoding.UTF8;
            settings.OmitXmlDeclaration = false;

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                CurrentDoc.Save(writer);
            }
        }

        public void ApplyBulkChange(DataTable dt, string column, double percentage, List<int> rowIndices = null)
        {
            if (dt == null || !dt.Columns.Contains(column))
            {
                return;
            }

            double multiplier = 1.0 + (percentage / 100.0);

            if (rowIndices != null && rowIndices.Count > 0)
            {
                foreach (int idx in rowIndices)
                {
                    if (idx >= 0 && idx < dt.Rows.Count)
                    {
                        UpdateRowCell(dt.Rows[idx], column, multiplier);
                    }
                }
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    UpdateRowCell(row, column, multiplier);
                }
            }
        }

        private static void UpdateRowCell(DataRow row, string column, double multiplier)
        {
            if (row.RowState == DataRowState.Deleted) return;

            string currentVal = row[column] != null ? row[column].ToString() : "0";
            double val = NumberParser.CleanNumber(currentVal);
            double newVal = val * multiplier;
            row[column] = newVal.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public void SetOrderKind(DataTable dt, int orderKindValue)
        {
            if (dt == null || !dt.Columns.Contains("id_orderkind"))
            {
                return;
            }

            string strVal = orderKindValue.ToString(CultureInfo.InvariantCulture);
            foreach (DataRow row in dt.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                row["id_orderkind"] = strVal;
            }
        }

        public Dictionary<string, string> GetParameterDefaults()
        {
            var dict = new Dictionary<string, string>();
            if (ParameterTemplate != null)
            {
                foreach (XElement param in ParameterTemplate.Elements("parameter"))
                {
                    XElement codeEl = param.Element("code");
                    XElement valEl = param.Element("value");
                    if (codeEl != null && !dict.ContainsKey(codeEl.Value))
                    {
                        dict[codeEl.Value] = valEl != null ? valEl.Value : "";
                    }
                }
            }
            return dict;
        }

        private static void SetOrAddElement(XElement parent, string elementName, string value)
        {
            XElement el = parent.Element(elementName);
            if (el != null)
            {
                el.Value = value;
            }
            else
            {
                parent.Add(new XElement(elementName, value));
            }
        }
    }

    internal static class EnumerableExtensions
    {
        public static T FirstOrDefault<T>(this IEnumerable<T> source)
        {
            if (source == null) return default(T);
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }
            return default(T);
        }

        public static List<T> ToList<T>(this IEnumerable<T> source)
        {
            return new List<T>(source);
        }
    }
}
