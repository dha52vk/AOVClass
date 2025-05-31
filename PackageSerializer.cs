using System.Text;

namespace AovClass
{
    public class PackageSerializer
    {
        /*
         * Note: + ket thuc:                    04 00 00 00 04 00 00 00
         *       + sau name jtcom,jtcus,jtarr:  03 00 00 00
         *       + giua name va jttype:         02 00 00 00
         *       + giua jttype:                 06 00 00 00
         *       + giua jttype va type:         08 00 00 00
         *       + giua type va value:          05 00 00 00
         *       + sau type:                    04 00 00 00
         */

        public PackageSerializer()
        {

        }

        public static PackageElement Deserialize(byte[] bytes)
        {
            int pointer = 0;
            int PackageSize = BitConverter.ToInt32(bytes, pointer); pointer += 4;

            PackageElement element = new();
            int nameEnd = pointer + BitConverter.ToInt32(bytes, pointer); pointer += 4;
            element.Name = bytes[pointer..nameEnd]; pointer = nameEnd;
            _ = pointer + BitConverter.ToInt32(bytes, pointer); pointer += 8;
            int jtTypeEnd = pointer + BitConverter.ToInt32(bytes, pointer); pointer += 8;
            element.JtType = bytes[pointer..jtTypeEnd]; pointer = jtTypeEnd;
            int typesEnd = pointer + BitConverter.ToInt32(bytes, pointer); pointer += 8;
            if (Encoding.UTF8.GetString(element.JtType) == "NULLY")
                return element;
            element.Type = bytes[pointer..typesEnd]; pointer = typesEnd + 8;

            switch (Encoding.UTF8.GetString(element.JtType))
            {
                case "JTArr":
                case "JTCus":
                case "JTCom":
                    if (pointer >= PackageSize)
                        break;
                    int childCount = BitConverter.ToInt32(bytes, pointer); pointer += 4;
                    element.Children = [];
                    for (int i = 0; i < childCount; i++)
                    {
                        int childEnd = pointer + BitConverter.ToInt32(bytes, pointer);
                        byte[] childBytes = bytes[pointer..childEnd];
                        PackageElement e = Deserialize(childBytes);
                        element.AddChild(e);
                        pointer = childEnd;
                    }
                    break;
                case "JTPri":
                case "JTEnum":
                    int valueEnd = typesEnd + BitConverter.ToInt32(bytes, typesEnd); pointer++;
                    element.Value = bytes[pointer..valueEnd];
                    break;
            }

            return element;
        }

        public static byte[] Serialize(PackageElement element)
        {
            List<int> indexStoreSize = [];
            List<byte> bytes = [.. new byte[] { 0, 0, 0, 0 }];
            indexStoreSize.Add(0);
            bytes.AddRange(BitConverter.GetBytes(element.Name.Length + 4));
            bytes.AddRange(element.Name);

            switch (Encoding.UTF8.GetString(element.JtType))
            {
                case "JTArr":
                case "JTCus":
                case "JTCom":
                    bytes.AddRange(BitConverter.GetBytes(24 + element.JtType.Length + element.Type.Length));
                    bytes.AddRange(BitConverter.GetBytes(2));
                    bytes.AddRange(BitConverter.GetBytes(8 + element.JtType.Length));
                    bytes.AddRange(BitConverter.GetBytes(6));
                    bytes.AddRange(element.JtType);
                    bytes.AddRange(BitConverter.GetBytes(8 + element.Type.Length));
                    bytes.AddRange(BitConverter.GetBytes(8));
                    bytes.AddRange(element.Type);
                    if (element.Children != null && element.Children.Count > 0)
                    {
                        bytes.AddRange(BitConverter.GetBytes(4)); indexStoreSize.Add(bytes.Count);
                        bytes.AddRange([0, 0, 0, 0]);
                        bytes.AddRange(BitConverter.GetBytes(element.Children.Count));
                        foreach (PackageElement p in element.Children)
                        {
                            bytes.AddRange(Serialize(p));
                        }
                    }
                    else
                    {
                        bytes.AddRange([4, 0, 0, 0, 4, 0, 0, 0]);
                    }
                    break;
                case "JTPri":
                case "JTEnum":
                    bytes.AddRange(BitConverter.GetBytes(33 + element.JtType.Length + element.Type.Length + element.Value.Length));
                    bytes.AddRange(BitConverter.GetBytes(3));
                    bytes.AddRange(BitConverter.GetBytes(8 + element.JtType.Length));
                    bytes.AddRange(BitConverter.GetBytes(6));
                    bytes.AddRange(element.JtType);
                    bytes.AddRange(BitConverter.GetBytes(8 + element.Type.Length));
                    bytes.AddRange(BitConverter.GetBytes(8));
                    bytes.AddRange(element.Type);
                    bytes.AddRange(BitConverter.GetBytes(9 + element.Value.Length));
                    bytes.AddRange([5, 0, 0, 0, 86]);
                    bytes.AddRange(element.Value);
                    bytes.AddRange([4, 0, 0, 0, 4, 0, 0, 0]);
                    break;
                case "NULLY":
                    bytes.AddRange([21, 0, 0, 0, 1, 0, 0, 0, 13, 0, 0, 0, 8, 0, 0, 0, 78, 85, 76, 76, 89, 4, 0, 0, 0, 4, 0, 0, 0]);
                    break;
                default:
                    throw new Exception("new JtTypeFound: " + Encoding.UTF8.GetString(element.JtType));

            }
            foreach (int index in indexStoreSize)
            {
                byte[] size = BitConverter.GetBytes(bytes.Count - index);
                for (int i = 0; i < 4; i++)
                {
                    bytes[index + i] = size[i];
                }
            }
            return [.. bytes];
        }
    }

    public class PackageElement
    {
        public byte[]? Name = null;
        public byte[]? Type = null;
        public byte[]? Value = null;
        public byte[]? JtType = null;
        public List<PackageElement>? Children = null;
#pragma warning disable IDE1006 // Naming Styles
        public string _Name
        {
            get { return Encoding.UTF8.GetString(Name); }
            set { Name = Encoding.UTF8.GetBytes(value); }
        }
        public string _Type
        {
            get { return Encoding.UTF8.GetString(Type); }
            set { Type = Encoding.UTF8.GetBytes(value); }
        }
        public string _Value
        {
            get { return Value != null ? Encoding.UTF8.GetString(Value) : ""; }
            set { Value = Encoding.UTF8.GetBytes(value); }
        }
        public string _JtType
        {
            get { return Encoding.UTF8.GetString(JtType); }
            set { JtType = Encoding.UTF8.GetBytes(value); }
        }
#pragma warning restore IDE1006 // Naming Styles

        public PackageElement() { }

        public void ModifyChildren(Func<PackageElement, PackageElement> replacement)
        {
            ModifyChildren(replacement, true);
        }

        public void ModifyChildren(Func<PackageElement, PackageElement> replacement, bool replaceInSubChild)
        {
            if (Children == null)
                return;
            for (int i = 0; i < Children.Count; i++)
            {
                PackageElement child = Children[i];
                child = replacement(child);
                if (replaceInSubChild)
                {
                    child.ModifyChildren(replacement);
                }
            }
        }

        public void AddChild(PackageElement child)
        {
            InsertChild(Children?.Count??0,child);
        }

        public void InsertChild(int pos, PackageElement element)
        {
            if (Children == null)
            {
                if (_JtType != "JTPri")
                {
                    Children = [];
                }
                else
                {
                    throw new Exception("Element is JTPri!! Can't add child");
                }
            }
            if (pos > Children.Count)
            {
                return;
            }
            if (_JtType.Equals("JTArr", StringComparison.CurrentCultureIgnoreCase)
                || _Type.Contains("Collections.Generic.List`1", StringComparison.CurrentCultureIgnoreCase)
                || element._Name.Equals("Element", StringComparison.CurrentCultureIgnoreCase))
            {
                element = element.Clone();
                element._Name = "Element";
                Children.Insert(pos, element);
            }
            else
            {
                int index = Children.FindIndex((child) => child._Name == element._Name);
                if (index >= 0)
                {
                    Children.RemoveAt(index);
                    if (index <= pos && pos > 0)
                        pos--;
                }
                Children.Insert(pos, element);
            }
        }

        public PackageElement Clone()
        {
            return new PackageElement()
            {
                Children = Children?.Select((child) => child.Clone()).ToList(),
                JtType = (byte[]?)JtType?.Clone(),
                Name = (byte[]?)Name?.Clone(),
                Type = (byte[]?)Type?.Clone(),
                Value = (byte[]?)Value?.Clone()
            };
        }
    }
}
