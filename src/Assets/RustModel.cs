using ProtoBuf;
using UnityEngine;

namespace Carbon.Client
{
	[ProtoContract]
	public partial class RustModel : MonoBehaviour
	{
		#region Editor

		public GameObject Prefab;

		#endregion

		[ProtoMember(1)]
		public string PrefabPath { get; set; }
	}
}
