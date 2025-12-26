using System;
using UnityEngine;
using DUG;

namespace WaiJigsaw.Data
{
	[PreferBinarySerialization]
	public partial class LevelGroupTable : KeyValueTable<int, LevelGroupTableRecord>
	{
		public LevelGroupTable() : base(nameof(LevelGroupTableRecord.GroupID))
		{}
	}

	[Serializable]
	public partial class LevelGroupTableRecord
	{
		public int GroupID;
		public int StartLevel;
		public int EndLevel;
		public string ImageName;
	}
}
