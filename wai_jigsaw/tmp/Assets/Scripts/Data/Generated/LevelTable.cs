using System;
using UnityEngine;
using DUG;

namespace WaiJigsaw.Data
{
	[PreferBinarySerialization]
	public partial class LevelTable : KeyValueTable<int, LevelTableRecord>
	{
		public LevelTable() : base(nameof(LevelTableRecord.levelID))
		{}
	}

	[Serializable]
	public partial class LevelTableRecord
	{
		public int levelID;
		public string ImageName;
		public int Rows;
		public int Cols;
	}
}
