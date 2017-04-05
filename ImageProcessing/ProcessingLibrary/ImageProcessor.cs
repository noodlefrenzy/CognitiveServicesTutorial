using Microsoft.ProjectOxford.Common.Contract;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageProcessingLibrary
{
    public class ImageProcessor
    {
        public static Task<ImageInsights> ProcessImageAsync(Func<Task<Stream>> imageStream, string imageId)
        {
            return Task.Run(async () =>
            {
                ImageAnalyzer analyzer = new ImageAnalyzer(imageStream);

                // trigger vision, face and emotion requests
                await Task.WhenAll(analyzer.AnalyzeImageAsync(), analyzer.DetectFacesAsync(detectFaceAttributes: true), analyzer.DetectEmotionAsync());

                // trigger face match against previously seen faces
                await analyzer.FindSimilarPersistedFacesAsync();

                ImageInsights result = new ImageInsights { ImageId = imageId };

                // assign computer vision results
                result.VisionInsights = new VisionInsights
                {
                    Caption = analyzer.AnalysisResult.Description?.Captions[0].Text,
                    Tags = analyzer.AnalysisResult.Tags.Select(t => t.Name).ToArray()
                };

                // assign face api and emotion api results
                List<FaceInsights> faceInsightsList = new List<FaceInsights>();
                foreach (var face in analyzer.DetectedFaces)
                {
                    FaceInsights faceInsights = new FaceInsights { FaceRectangle = face.FaceRectangle };

                    SimilarFaceMatch similarFaceMatch = analyzer.SimilarFaceMatches.FirstOrDefault(s => s.Face.FaceId == face.FaceId);
                    if (similarFaceMatch != null)
                    {
                        faceInsights.UniqueFaceId = similarFaceMatch.SimilarPersistedFace.PersistedFaceId;
                    }

                    Emotion faceEmotion = CoreUtil.FindFaceClosestToRegion(analyzer.DetectedEmotion, face.FaceRectangle);
                    if (faceEmotion != null)
                    {
                        faceInsights.TopEmotion = faceEmotion.Scores.ToRankedList().First().Key;
                    }

                    faceInsightsList.Add(faceInsights);
                }

                result.FaceInsights = faceInsightsList.ToArray();

                return result;
            });
        }
    }
}
