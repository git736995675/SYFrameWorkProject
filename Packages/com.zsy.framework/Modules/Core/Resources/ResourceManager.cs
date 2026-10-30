using System.Threading.Tasks;
using UnityEngine;

namespace ZSYFramework.Core
{
	/// <summary>
/// 功能描述：
/// </summary>
	public class ResourceManager : MonoSingleton<ResourceManager>
	{
		public static T Load<T>(string path) where T : UnityEngine.Object
		{
			return Resources.Load<T>(path);
		}

		public static async Task<T> LoadAsync<T>(string path) where T : UnityEngine.Object
		{
			var request = Resources.LoadAsync<T>(path);
			while (!request.isDone)
			{
				await Task.Yield();
			}
			return request.asset as T;
		}
	}
}
