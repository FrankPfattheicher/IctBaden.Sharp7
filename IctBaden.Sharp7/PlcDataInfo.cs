using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Sharp7;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Sharp7
{
    public class PlcDataInfo
    {
        public string Name { get; private set; }
        public string ItemId { get; private set; }

        public int PlcArea { get; private set; }
        public int PlcWordLen { get; private set; }

        public int DbNumber { get; private set; }
        public int Offset { get; private set; }
        public int ByteCount { get; private set; }
        public int Bit { get; private set; }
        public int MaxStringLength { get; private set; }
        public PlcDataTypes PlcDataType { get; private set; }


        public PlcDataInfo(string name, string itemId)
        {
            Name = name;
            ItemId = itemId;
            ParseId();
        }

        private void ParseId()
        {
            // DB12,X4.4
            // DB12,INT12
            // DB12,DINT48
            // DB12,STRING660.10

            //                         1        2       3       4  5
            var parser = new Regex(@"DB([0-9]+),([A-Z]+)([0-9]+)(\.([0-9]+))?");
            var parsed = parser.Match(ItemId);
            if (!parsed.Success)
            {
                throw new ArgumentException("Unable to parse item id " + ItemId);
            }

            PlcArea = S7Consts.S7AreaDB;
            DbNumber = int.Parse(parsed.Groups[1].Value);
            Offset = int.Parse(parsed.Groups[3].Value);

            var dataTypeText = parsed.Groups[2].Value;
            if (!Enum.TryParse(dataTypeText, out PlcDataTypes dataType))
            {
                throw new ArgumentException("Unsupported data type in item id " + ItemId);
            }

            PlcDataType = dataType;

            try
            {
                switch (dataType)
                {
                    case PlcDataTypes.X:
                        ByteCount = 1;
                        Bit = int.Parse(parsed.Groups[5].Value);
                        PlcWordLen = S7Consts.S7WLBit;
                        break;
                    case PlcDataTypes.B:
                        ByteCount = 1;
                        PlcWordLen = S7Consts.S7WLByte;
                        break;
                    case PlcDataTypes.INT:
                        ByteCount = 2;
                        PlcWordLen = S7Consts.S7WLInt;
                        break;
                    case PlcDataTypes.DINT:
                        ByteCount = 4;
                        PlcWordLen = S7Consts.S7WLDInt;
                        break;
                    case PlcDataTypes.DT:
                        ByteCount = 8;
                        PlcWordLen = S7Consts.S7WLTimer;
                        break;
                    case PlcDataTypes.STRING:
                        MaxStringLength = int.Parse(parsed.Groups[5].Value);
                        ByteCount = MaxStringLength + 2;   // [MaxLength] [ActualLength] [chars][chars][chars][chars][chars]...
                        break;
                    default:
                        throw new ArgumentException("Unsupported data type in item id " + ItemId);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"PlcDataInfo.ParseId({Name}) Id={ItemId}: {ex.Message}");
                throw new ArgumentException("Invalid data type specification in item id " + ItemId);
            }

        }

        public override string ToString()
        {
            return Name;
        }
    }
}
