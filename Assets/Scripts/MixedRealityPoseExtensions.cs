using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public static class MixedRealityPoseExtensions
{
    public static MixedRealityPose Multiply(this MixedRealityPose self, MixedRealityPose other)
    {
        return new MixedRealityPose(self.Position + self.Rotation * other.Position, self.Rotation * other.Rotation);
    }

    public static Vector3 Multiply(this MixedRealityPose self, Vector3 v)
    {
        return self.Position + self.Rotation * v;
    }

    public static Quaternion Multiply(this MixedRealityPose self, Quaternion q)
    {
        return self.Rotation * q;
    }

    public static MixedRealityPose Inverse(this MixedRealityPose self)
    {
        Quaternion invRot = Quaternion.Inverse(self.Rotation);
        return new MixedRealityPose(-(invRot * self.Position), invRot);
    }
}