using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityClassLibrary
{
    public class ReferentialStringComparer<T> : IComparer<string>
    {
        private IDictionary<string, T> _dictionary;
        private string _compareParam;
        private List<string> _targetSubstrings;

        public ReferentialStringComparer(IDictionary<string, T> refDictionary, string parameterToCompareOn, List<string> compareTargetSubstrings)
        {
            _dictionary = refDictionary;
            _compareParam = parameterToCompareOn;
            _targetSubstrings = compareTargetSubstrings;
        }

        public int Compare(string? x, string? y)
        {
            dynamic actualX = _dictionary[x].GetType().GetProperty(_compareParam).GetValue(_dictionary[x]);
            dynamic actualY = _dictionary[y].GetType().GetProperty(_compareParam).GetValue(_dictionary[y]);

            string targetX = "";
            string targetY = "";

            foreach (string compareTarget in _targetSubstrings)
            {
                if(actualX.ToLower().Contains(compareTarget.ToLower()) && targetX == "")
                    targetX = compareTarget;
                if (actualY.ToLower().Contains(compareTarget.ToLower()) && targetY == "")
                    targetY = compareTarget;
            }

            if((targetX != "" || targetY != "") && targetX != targetY)
            {
                if(targetX != "")
                    actualX = targetX;
                if(targetY != "")
                    actualY = targetY;
            }

            return actualX.CompareTo(actualY);
        }
    }
}
