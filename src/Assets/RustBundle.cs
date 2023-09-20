using System.Collections.Generic;
using ProtoBuf;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client
{
	[ProtoContract]
	public partial class RustBundle
	{
		[ProtoMember(1)]
		public Dictionary<string, List<RustComponent>> Components = new Dictionary<string, List<RustComponent>>();

		[ProtoMember(2)]
		public List<RustPrefab> RustPrefabs = new List<RustPrefab>();
	}
}
