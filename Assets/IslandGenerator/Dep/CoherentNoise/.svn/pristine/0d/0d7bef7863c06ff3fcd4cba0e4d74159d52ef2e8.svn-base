using CoherentNoise.Interpolation;
using UnityEngine;

namespace CoherentNoise.Generation
{
	/// <summary>
	/// This is the same noise as <see cref="ValueNoise"/>, but it does not change in Z direction. This is more efficient if you're only interested in 2D noise anyway.
	/// </summary>
	public class ValueNoise2D : Generator
	{
		private readonly CoherentNoise.LatticeNoise m_Source;
		private readonly SCurve m_SCurve;

		/// <summary>
		/// Create new generator with specified seed
		/// </summary>
		/// <param name="seed">noise seed</param>
		public ValueNoise2D(int seed)
			: this(seed, null)
		{

		}

		/// <summary>
		/// Create new generator with specified seed and interpolation algorithm. Different interpolation algorithms can make noise smoother at the expense of speed.
		/// </summary>
		/// <param name="seed">noise seed</param>
		/// <param name="sCurve">Interpolator to use. Can be null, in which case default will be used</param>
		public ValueNoise2D(int seed, SCurve sCurve)
		{
			m_Source = new CoherentNoise.LatticeNoise(seed);
			m_SCurve = sCurve;
		}

		private SCurve SCurve { get { return m_SCurve ?? SCurve.Default; } }

		/// <summary>
		/// Noise period. Used for repeating (seamless) noise.
		/// When Period &gt;0 resulting noise pattern repeats exactly every Period, for all coordinates.
		/// </summary>
		public int Period { get { return m_Source.Period; } set { m_Source.Period = value; } }

		#region Implementation of Noise

		/// <summary>
		/// Returns noise value at given point. 
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="z">Z coordinate</param>
		/// <returns>Noise value</returns>
		public override float GetValue(float x, float y, float z)
		{
			int ix = Mathf.FloorToInt(x);
			int iy = Mathf.FloorToInt(y);

			// interpolate the coordinates instead of values - it's way faster
			float xs = SCurve.Interpolate(x - ix);
			float ys = SCurve.Interpolate(y - iy);

			// THEN we can use linear interp to find our value - biliear actually

			float n0 = m_Source.GetValue(ix, iy, 0);
			float n1 = m_Source.GetValue(ix + 1, iy, 0);
			float ix0 = Mathf.Lerp(n0, n1, xs);

			n0 = m_Source.GetValue(ix, iy + 1, 0);
			n1 = m_Source.GetValue(ix + 1, iy + 1, 0);
			float ix1 = Mathf.Lerp(n0, n1, xs);

			return Mathf.Lerp(ix0, ix1, ys);
		}


		#endregion
	}
}