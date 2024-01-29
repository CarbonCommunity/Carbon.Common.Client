/*
 *
 * Copyright (c) 2022-2024 Carbon Community  
 * All rights reserved.
 *
 */

using System.Collections.Generic;
using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract]
public class ClientModifications : BasePacket
{
	[ProtoMember(1)]
	public List<Entry> Entries { get; set; } = new();

	public override void Dispose()
	{
		Entries.Clear();
		Entries = null;
	}

	[ProtoContract(InferTagFromName = true)]
	public class Entry
	{
		public string Identifier { get; set; }
		public string Path { get; set; }
		public string Value { get; set; }
		public string Component { get; set; }
		public Types Type { get; set; }

		public enum Types
		{
			NONE = -1,
			Entity,
			ItemDefinition,
			Static,
			Prefab
		}
	}
}
