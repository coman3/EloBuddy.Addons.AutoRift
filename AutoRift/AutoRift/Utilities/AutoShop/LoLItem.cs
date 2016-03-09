namespace AutoRift.Utilities.AutoShop
{
    public class LoLItem
    {
        public readonly int BaseGold;
        public readonly string Cq;
        public readonly int Depth;
        public readonly string Description;

        public readonly int[] FromItems;
        public readonly string Groups;

        public readonly int Id;
        public readonly int[] IntoItems;

        public readonly int[] Maps;
        public readonly string Name;
        public readonly string Plaintext;

        public readonly bool Purchasable;
        public readonly string RequiredChampion;
        public readonly string SanitizedDescription;
        public readonly int SellGold;
        public readonly string[] Tags;
        public readonly int TotalGold;

        public LoLItem(string name, string description, string sanitizedDescription, string plaintext, int id,
            int baseGold, int totalGold, int sellGold, bool purchasable
            , string requiredChampion, int[] maps, int[] fromItems, int[] intoItems, int depth, string[] tags, string cq,
            string groups)
        {
            this.Name = name;
            this.Description = description;
            this.SanitizedDescription = sanitizedDescription;
            this.Plaintext = plaintext;
            this.Id = id;
            this.BaseGold = baseGold;
            this.TotalGold = totalGold;
            this.SellGold = sellGold;
            this.Purchasable = purchasable;
            this.RequiredChampion = requiredChampion;
            this.Maps = maps;
            this.FromItems = fromItems;
            this.IntoItems = intoItems;
            this.Depth = depth;
            this.Tags = tags;
            this.Cq = cq;
            this.Groups = groups;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool IsHealthlyConsumable()
        {
            return Id == 2003 || Id == 2009 || Id == 2010;
        }
    }
}