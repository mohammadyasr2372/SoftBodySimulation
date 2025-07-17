using UnityEngine;
class Spring
    {
        public int a, b;
        public float restLength, originalRest, maxLength, stress;

        public Spring(int i, int j, float rest, float maxStretch)
        {
            a = i; b = j; restLength = originalRest = rest;
            maxLength = rest * maxStretch;
            stress = 0f;
        }
    }