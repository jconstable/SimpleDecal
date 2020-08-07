using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDecal
{
    public static class VectorExtensions
    {
        public static Vector3 LocalToWorld(this Vector3 v, Transform t)
        {
            Vector3 v2 = t.localToWorldMatrix * v;
            return t.position + v2;
        }

        public static Vector3 WorldToLocal(this Vector3 v, Transform t)
        {
            Vector3 v2 = v + -t.position;
            return t.worldToLocalMatrix * v2;
        }

        public static bool Approximately(this Quaternion a, Quaternion b)
        {
            return Mathf.Approximately(a.w, b.w) &&
                Mathf.Approximately(a.x, b.x) &&
                Mathf.Approximately(a.y, b.y) &&
                Mathf.Approximately(a.z, b.z);

        }

        public static bool Approximately(this Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                Mathf.Approximately(a.y, b.y) &&
                Mathf.Approximately(a.z, b.z);
        }
    }
}