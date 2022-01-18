using System.Collections.Generic;

public class Data
{
    public Data()
    {
        shop = new Dictionary<int, Item>();
        economy = new Dictionary<ulong, int>();
        itemEconomy = new Dictionary<ulong, List<Item>>();
        itemCount = 0;
    }
    public int itemCount;
    public Dictionary<int, Item> shop;
    public Dictionary<ulong, int> economy;
    public Dictionary<ulong, List<Item>> itemEconomy;
}