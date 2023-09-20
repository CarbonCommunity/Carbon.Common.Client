using UnityEngine;

namespace Carbon.Client
{
	public partial class RustPrefab
	{
		public GameObject Lookup()
		{
			return GameManager.server.FindPrefab(Path);
		}
		public void Apply(GameObject target)
		{
			target.transform.SetPositionAndRotation(Position.ToVector3(), Rotation.ToQuaternion());
			target.transform.localScale = Scale.ToVector3();
		}
	}
}
