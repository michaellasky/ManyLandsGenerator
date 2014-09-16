using System;

namespace CoherentNoise.Interpolation
{
	///<summary>
	/// Linear interpolator is the fastest and has the lowest quality, only ensuring continuity of noise values, not their derivatives.
	///</summary>
	internal class LinearSCurve : SCurve
	{
		#region Overrides of Interpolator

		public override float Interpolate(float t)
		{
			return t;
		}

		#endregion
	}
}