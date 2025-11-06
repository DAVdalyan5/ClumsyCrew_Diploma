using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Helpers
{
    /// <summary>
    /// Group everything here with respectible shits
    /// </summary>
    public static class Extensions
    { 
        public static bool IsNullOrEmpty(this string text)
        {
            return string.IsNullOrEmpty(text);
        }

        public static bool IsJsonEmpty(this string text)
        {
            return text == "{}";
        }

        public static bool IsNullOrDefault(this int val)
        {
            return val == default || val == 0;
        }

        /// <summary>
        /// valmin/valmax are current range of 
        /// min/max are desired range to normalise into
        /// </summary>
        public static float NormalizeInto(this float val, float valmin, float valmax, float min, float max)
        {
            return (((val - valmin) / (valmax - valmin)) * (max - min)) + min;
        }

        /// <summary>
        /// Normalise Number into the range [0,1] by default
        /// </summary>
        public static double Normalize(this double val, double min = 0, double max = 1) => min + val * (max - min);

        /// <summary>
        /// Converts normalized 0-1 to +20 -80 logarithmic
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static float GetLogarithmicDecibel(this float val)
        {
            const float minDecibel = -80f;
            const float offset = 20f;

            var res = offset * Mathf.Log10(val);

            if (float.IsInfinity(res))
            {
                return minDecibel;
            }

            return res;
        }

        /// <summary>
        /// Get Bool from int;
        /// True when > 0
        /// </summary>
        public static bool GetBool(this int val)
        {
            return val > 0;
        }

        public static int GetInt(this bool val)
        {
            return val ? 1 : 0;
        }

        public static bool IsInRange(this float val, float rangeMin, float rangeMax)
        {
            return val >= rangeMin && val <= rangeMax;
        }

        public static bool IsInErrorRange(this float val, float compareValue, float error)
        {
            return val >= val - error && val <= val + error;
        }

        public static bool IsInRangeExclusive(this float val, float rangeMin, float rangeMax)
        {
            return val > rangeMin && val < rangeMax;
        }
    }
}
