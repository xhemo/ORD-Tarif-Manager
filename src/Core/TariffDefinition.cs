using System;
using System.Collections.Generic;

namespace OrdTarifManager.Core
{
    public class TariffDefinition
    {
        public string spec_name { get; set; }
        public string description { get; set; }
        public string unit_code { get; set; }
        public string currency_code { get; set; }
        public int id_orderkind_default { get; set; }
        public List<string> columns { get; set; }
        public Dictionary<string, object> defaults { get; set; }

        public TariffDefinition()
        {
            spec_name = "";
            description = "";
            unit_code = "";
            currency_code = "EUR";
            id_orderkind_default = 2;
            columns = new List<string>();
            defaults = new Dictionary<string, object>();
        }
    }

    public class TariffMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
        public string Spec { get; set; }
        public string CurrencyCode { get; set; }
        public string UnitCode { get; set; }
        public int OrderKind { get; set; }

        public TariffMetadata()
        {
            Id = "";
            Name = "";
            ValidFrom = DateTime.Today.ToString("yyyy-MM-dd");
            ValidTo = "2099-12-31";
            Spec = "";
            CurrencyCode = "EUR";
            UnitCode = "";
            OrderKind = 2;
        }
    }

    public class ColumnDefinitionItem
    {
        public string ColumnName { get; set; }
        public string DefaultValue { get; set; }

        public ColumnDefinitionItem(string name, string defVal)
        {
            ColumnName = name;
            DefaultValue = defVal;
        }
    }
}
