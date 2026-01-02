using System;
using UnityEngine;
using DUG;

namespace WaiJigsaw.Data
{
	[PreferBinarySerialization]
	public partial class ItemTable : KeyValueTable<ITEM_TYPE, ItemTableRecord>
	{
		public ItemTable() : base(nameof(ItemTableRecord.Item_Type))
		{}
	}

	[Serializable]
	public partial class ItemTableRecord
	{
		public ITEM_TYPE Item_Type;
		public ITEM_CATEGORY Item_Category;
		public ITEM_GETTYPE Item_GetType;
		public int Item_Price;
		public string Item_Icon;
	}
}
