using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceHelpers
{
    public class CoreUtil
    {
        public static Emotion FindFaceClosestToRegion(IEnumerable<Emotion> emotion, FaceRectangle region)
        {
            return emotion?.Where(e => CoreUtil.AreFacesPotentiallyTheSame(e.FaceRectangle, region))
                                  .OrderBy(e => Math.Abs(region.Left - e.FaceRectangle.Left) + Math.Abs(region.Top - e.FaceRectangle.Top)).FirstOrDefault();
        }

        public static bool AreFacesPotentiallyTheSame(Rectangle face1, FaceRectangle face2)
        {
            return AreFacesPotentiallyTheSame((int)face1.Left, (int)face1.Top, (int)face1.Width, (int)face1.Height, face2.Left, face2.Top, face2.Width, face2.Height);
        }

        public static bool AreFacesPotentiallyTheSame(int face1X, int face1Y, int face1Width, int face1Height,
                                                       int face2X, int face2Y, int face2Width, int face2Height)
        {
            double distanceThresholdFactor = 1;
            double sizeThresholdFactor = 0.5;

            // See if faces are close enough from each other to be considered the "same"
            if (Math.Abs(face1X - face2X) <= face1Width * distanceThresholdFactor &&
                Math.Abs(face1Y - face2Y) <= face1Height * distanceThresholdFactor)
            {
                // See if faces are shaped similarly enough to be considered the "same"
                if (Math.Abs(face1Width - face2Width) <= face1Width * sizeThresholdFactor &&
                    Math.Abs(face1Height - face2Height) <= face1Height * sizeThresholdFactor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
