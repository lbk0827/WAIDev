using System;
using UnityEngine;
using DUG;

namespace WaiJigsaw.Data
{
	[PreferBinarySerialization]
	public partial class CardTable : KeyValueTable<int, CardTableRecord>
	{
		public CardTable() : base(nameof(CardTableRecord.CardID))
		{}
	}

	[Serializable]
	public partial class CardTableRecord
	{
		public int CardID;
		public string CardName;
		public string CardBackSprite;
	}
}
