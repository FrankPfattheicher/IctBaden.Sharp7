using System;
using System.Text.RegularExpressions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Sharp7
{
    public class PlcDataInfo
    {
        public string Name { get; private set; }
        public string ItemId { get; private set; }

        public int DbNumber { get; private set; }
        public int Offset { get; private set; }
        public int Size { get; private set; }
        public int Bit { get; private set; }
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

            //                                1        2       3       4  5
            var parser = new Regex(@"DB([0-9]+),([A-Z]+)([0-9]+)(\.([0-9]+))?");
            var parsed = parser.Match(ItemId);
            if (!parsed.Success)
            {
                throw new ArgumentException("Unable to parse item id " + ItemId);
            }

            DbNumber = int.Parse(parsed.Groups[1].Value);
            Offset = int.Parse(parsed.Groups[3].Value);

            var dataTypeText = parsed.Groups[2].Value;
            if (!Enum.TryParse(dataTypeText, out PlcDataTypes dataType))
            {
                throw new ArgumentException("Unsupported data type in item id " + ItemId);
            }

            PlcDataType = dataType;

            switch (dataType)
            {
                case PlcDataTypes.X:
                    Size = 1;
                    Bit = int.Parse(parsed.Groups[5].Value);
                    break;
                case PlcDataTypes.B:
                    Size = 1;
                    break;
                case PlcDataTypes.INT:
                    Size = 2;
                    break;
                case PlcDataTypes.DINT:
                    Size = 4;
                    break;
                case PlcDataTypes.DT:
                    Size = 8;
                    break;
                case PlcDataTypes.STRING:
                    Size = int.Parse(parsed.Groups[5].Value);
                    break;
                default:
                    throw new ArgumentException("Unsupported data type in item id " + ItemId);
            }

        }

        public override string ToString()
        {
            return Name;
        }
    }
}
