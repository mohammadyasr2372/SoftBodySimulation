using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
class VertexPoint
    {
        public Vector3 current, previous, original, velocity, permanentOffset;
        public bool isFixed, isInternal;
        public int idx;
        public List<int> neighbors = new List<int>();

        public VertexPoint(Vector3 pos, int index, bool internalPt = false)
        {
            original = current = previous = pos;
            velocity = permanentOffset = Vector3.zero;
            isInternal = internalPt;
            isFixed = false;
            idx = index;
        }
    }