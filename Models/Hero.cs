using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AovClass.Models
{
    public class Hero
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public List<Skin>? Skins { get; set; }

        public string IconURL { get { return $"https://github.com/dha52vk/AOVResources/raw/main/Normal/{Id}1.jpg"; } }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
