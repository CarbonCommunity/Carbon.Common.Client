namespace Carbon.Client.Extensions
{
	public static class ModelEx
	{
		public static RustPrefab.ServerModel ToCustomModel(this BaseEntity entity)
		{
			if (RustPrefab.ServerModel.Models.TryGetValue(entity, out var model))
			{
				return model;
			}

			return null;
		}
	}
}
