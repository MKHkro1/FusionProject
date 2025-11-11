using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace NoHeadUltimateHorse.BepInEx
{
	/// 植物取消限伤标记
	public class UncappedPlantDamageComponent : MonoBehaviour
	{
		public UncappedPlantDamageComponent() : base(ClassInjector.DerivedConstructorPointer<UncappedPlantDamageComponent>()) =>
			ClassInjector.DerivedConstructorBody(this);

		public UncappedPlantDamageComponent(System.IntPtr ptr) : base(ptr) { }
	}
}

