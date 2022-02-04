using System;
using System.Collections.Generic;

namespace NXIngest
{
    public abstract class MappingCommand { }

    public class StartTable : MappingCommand
    {
        public readonly string TableName;
        public readonly Dictionary<string, string> Attributes;

        public StartTable(
            string tableName,
            Dictionary<string, string> attributes)
        {
            TableName = tableName;
            Attributes = attributes;
        }
    }

    public class EndTable : MappingCommand { }

    public enum MappingValueType
    {
        Fix,
        Mix,
        Nexus,
        Time,
        Sys
    }

    public abstract class AddElementWithValue : MappingCommand
    {
        public readonly string Value;
        public readonly MappingValueType ValueType;

        protected AddElementWithValue(string value, string valueType)
        {
            Value = value;
            ValueType =
                Enum.Parse<MappingValueType>(valueType.Capitalized());
        }
    }

    public class AddRecord : AddElementWithValue
    {
        public readonly string Name;

        public AddRecord(
            string name,
            string value,
            string valueType) : base(value, valueType)
        {
            Name = name;
        }
    }

    public class AddParameter : AddElementWithValue
    {
        public readonly string Name;
        public readonly string Units;
        public readonly MappingValueType UnitsType;
        public readonly string Description;
        public readonly bool IsNum;

        public AddParameter(
            string name,
            string value,
            string valueType,
            string units,
            string unitsType,
            string description,
            string type) : base(value, valueType)
        {
            Name = name;
            Units = units ?? "N/A";
            UnitsType = unitsType == null
                ? MappingValueType.Fix
                : Enum.Parse<MappingValueType>(unitsType.Capitalized());
            Description = description;
            IsNum = type == "param_num";
        }
    }

    public class AddKeywords : AddElementWithValue
    {
        public AddKeywords(string value, string valueType)
            : base(value, valueType) { }
    }
}
